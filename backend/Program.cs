using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Logging;
using Microsoft.AspNetCore.DataProtection;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Services;
using Serilog;
using Microsoft.OpenApi.Models;

// Configurar Serilog de forma bem inicial antes do host build
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ShowPII só em Development para não expor dados sensíveis em logs de homolog/prod
    IdentityModelEventSource.ShowPII = builder.Configuration.GetValue<bool>("Logging:ShowPII");

    // Substituir logger padrão pelo Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        var apiTitle = builder.Configuration["Swagger:Title"] ?? "Gerenciamento Endereço API";
        var apiVersion = builder.Configuration["Swagger:Version"] ?? "v1";
        c.SwaggerDoc(apiVersion, new OpenApiInfo
        {
            Title = apiTitle,
            Version = apiVersion,
            Description = "API REST para consulta dos endereços cadastrados. Autenticação via cookie de sessão " +
                          "(faça login pela interface web primeiro — o Swagger UI reutiliza a mesma sessão do navegador).",
            Contact = new OpenApiContact { Name = "Gerenciamento de Endereços" }
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Persistir chaves do Data Protection no SQL Server (sobrevive a qualquer rebuild)
var dpAppName = builder.Configuration["DataProtection:ApplicationName"] ?? "GerenciamentoEndereco";
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName(dpAppName);

// Setup Authentication with OpenID Connect (Keycloak)
builder.Services.AddAuthentication(options => 
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];

    // Discovery automático (MetadataAddress) não funciona bem aqui: o Keycloak embute
    // SEMPRE o mesmo hostname (KC_HOSTNAME) em todos os endpoints do discovery document,
    // mas o navegador do usuário e o backend-api enxergam o Keycloak por hostnames
    // diferentes (localhost:8089 vs keycloak:8080, dentro da rede Docker). Por isso
    // montamos a configuração manualmente: endpoints usados pelo NAVEGADOR (auth,
    // logout) apontam pro host externo; endpoints chamados SERVER-TO-SERVER pelo
    // backend (token, userinfo, jwks) apontam pro host interno do container.
    var externalRealmUrl = $"http://{builder.Configuration["Keycloak:ExternalHost"] ?? "localhost:8089"}/realms/gerenciamento-endereco";
    var internalRealmUrl = $"http://{builder.Configuration["Keycloak:InternalHost"] ?? "keycloak:8080"}/realms/gerenciamento-endereco";

    var oidcConfig = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
    {
        Issuer = externalRealmUrl,
        AuthorizationEndpoint = $"{externalRealmUrl}/protocol/openid-connect/auth",
        EndSessionEndpoint = $"{externalRealmUrl}/protocol/openid-connect/logout",
        TokenEndpoint = $"{internalRealmUrl}/protocol/openid-connect/token",
        UserInfoEndpoint = $"{internalRealmUrl}/protocol/openid-connect/userinfo",
        JwksUri = $"{internalRealmUrl}/protocol/openid-connect/certs"
    };

    // Busca as chaves de assinatura (JWKS) do host interno, com retry — o Keycloak pode
    // ainda estar de boot quando o backend-api inicia (mesma rede Docker, containers
    // diferentes).
    using (var jwksClient = new HttpClient())
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var jwksJson = jwksClient.GetStringAsync(oidcConfig.JwksUri).GetAwaiter().GetResult();
                var jwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);
                foreach (var key in jwks.GetSigningKeys())
                {
                    oidcConfig.SigningKeys.Add(key);
                }
                break;
            }
            catch when (attempt < 10)
            {
                Thread.Sleep(2000);
            }
        }
    }

    options.Configuration = oidcConfig;

    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
    
    // O ID token do Keycloak já inclui os claims necessários (name, email,
    // preferred_username, realm_access.roles) — não precisamos da chamada extra
    // ao userinfo endpoint (que, por rodar server-to-server contra o host interno,
    // estava sendo rejeitada com 401 pelo Keycloak).
    options.GetClaimsFromUserInfoEndpoint = false;
    options.Scope.Add("email");
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = builder.Configuration.GetValue<bool>("Keycloak:ValidateIssuer"),
        ValidateAudience = false,
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };

    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            // O Keycloak manda os papéis do usuário como um claim "realm_access" cujo
            // valor é um objeto JSON bruto: {"roles":["ADMIN","USUARIO"]}. Expandimos
            // isso em claims individuais de role (ClaimTypes.Role + "roles") pra
            // funcionar com IsInRole(...) e com nossas políticas de autorização.
            if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            {
                void AddRolesFromJson(string? rawJson)
                {
                    if (string.IsNullOrEmpty(rawJson)) return;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
                        if (!doc.RootElement.TryGetProperty("roles", out var rolesElement)) return;

                        foreach (var roleEl in rolesElement.EnumerateArray())
                        {
                            var roleVal = roleEl.GetString();
                            if (string.IsNullOrEmpty(roleVal)) continue;

                            if (!identity.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == roleVal))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleVal));
                            }
                            if (!identity.HasClaim(c => c.Type == "roles" && c.Value == roleVal))
                            {
                                identity.AddClaim(new System.Security.Claims.Claim("roles", roleVal));
                            }
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        Log.Error(ex, "Erro ao interpretar claim de roles do Keycloak.");
                    }
                }

                AddRolesFromJson(context.Principal.FindFirst("realm_access")?.Value);
            }
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProvider = context =>
        {
            // Não precisa reescrever hostname aqui: AuthorizationEndpoint/EndSessionEndpoint
            // já são montados com o host externo em options.Configuration, acima.

            // Força a interface do Keycloak a ficar em português
            context.ProtocolMessage.SetParameter("kc_locale", "pt-BR");
            context.ProtocolMessage.SetParameter("ui_locales", "pt-BR");

            return Task.CompletedTask;
        }
    };
});

// Registrar serviços
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>();
builder.Services.AddHttpClient<IViaCepService, ViaCepService>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();

// Configurar Políticas de Autorização integradas ao Keycloak
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EnderecoRead", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.read") ||
        context.User.Identity?.IsAuthenticated == true
    ));

    options.AddPolicy("EnderecoWrite", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.write") ||
        context.User.Identity?.IsAuthenticated == true
    ));

    options.AddPolicy("EnderecoDelete", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.delete") ||
        context.User.Identity?.IsAuthenticated == true
    ));

    options.AddPolicy("EnderecoExport", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.export") ||
        context.User.Identity?.IsAuthenticated == true
    ));

    options.AddPolicy("DocsRead", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "admin")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "docs.read")
    ));

    options.AddPolicy("UsuariosManage", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "admin")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "usuarios.manage")
    ));
});

var app = builder.Build();

// Aplicar Migrations automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

    // Configurar o pipeline do Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var swaggerVersion = builder.Configuration["Swagger:Version"] ?? "v1";
        var swaggerTitle = builder.Configuration["Swagger:Title"] ?? "Gerenciamento Endereço API";
        c.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", $"{swaggerTitle} {swaggerVersion}");
    });

    // Serilog Request Logging
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar.");
}
finally
{
    Log.CloseAndFlush();
}
