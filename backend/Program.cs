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
    
    var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
    if (!string.IsNullOrEmpty(metadataAddress))
    {
        options.MetadataAddress = metadataAddress;
    }

    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
    
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("email");
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = builder.Configuration.GetValue<bool>("Keycloak:ValidateIssuer"),
        ValidateAudience = false,
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };

    options.ClaimActions.MapCustomJson("_temp_realm_roles", json =>
    {
        if (json.TryGetProperty("realm_access", out var realmAccess) &&
            realmAccess.TryGetProperty("roles", out var rolesElement))
        {
            return rolesElement.GetRawText();
        }
        return null;
    });

    options.ClaimActions.MapCustomJson("_temp_client_roles", json =>
    {
        if (json.TryGetProperty("resource_access", out var resourceAccess) && 
            resourceAccess.TryGetProperty("app-csharp", out var appElement) &&
            appElement.TryGetProperty("roles", out var rolesElement))
        {
            return rolesElement.GetRawText();
        }
        return null;
    });

    // Ajusta as URLs para o navegador do usuário (que está fora do Docker)
    var keycloakInternalHost = builder.Configuration["Keycloak:InternalHost"] ?? "gerenc-keycloak:8080";
    var keycloakExternalHost = builder.Configuration["Keycloak:ExternalHost"] ?? "localhost:8089";
    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity)
            {
                var currentClaims = context.Principal.Claims.ToList();
                foreach (var claim in currentClaims)
                {
                    if (claim.Type == "groups" || claim.Type == "roles" || claim.Type == "group" || claim.Type == System.Security.Claims.ClaimTypes.Role)
                    {
                        var roleVal = claim.Value;
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

                var accessToken = context.TokenEndpointResponse?.AccessToken;
                if (!string.IsNullOrEmpty(accessToken))
                {
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(accessToken))
                        {
                            var jwt = handler.ReadJwtToken(accessToken);
                            foreach (var claim in jwt.Claims)
                            {
                                if (claim.Type == "groups" || claim.Type == "roles")
                                {
                                    if (!identity.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == claim.Value))
                                    {
                                        identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, claim.Value));
                                    }
                                    if (!identity.HasClaim(c => c.Type == "roles" && c.Value == claim.Value))
                                    {
                                        identity.AddClaim(new System.Security.Claims.Claim("roles", claim.Value));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Erro ao extrair roles do access token.");
                    }
                }
            }
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProvider = context =>
        {
            var keycloakInternalHost = builder.Configuration.GetValue<string>("Keycloak:InternalHost") ?? "authentik-server:9000";
            var keycloakExternalHost = builder.Configuration.GetValue<string>("Keycloak:ExternalHost") ?? "localhost:8089";

            if (!string.IsNullOrEmpty(context.ProtocolMessage.IssuerAddress))
            {
                context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                    .Replace("authentik-server:9000", keycloakExternalHost)
                    .Replace(keycloakInternalHost, keycloakExternalHost);
            }
            
            // Força a interface do Authentik a ficar em português
            context.ProtocolMessage.SetParameter("ui_locales", "pt-BR");

            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress
                .Replace("authentik-server:9000", keycloakExternalHost)
                .Replace(keycloakInternalHost, keycloakExternalHost);

            // Workaround: Authentik 2026.5.x rejeita o end-session request com "Bad Request"
            // quando post_logout_redirect_uri é enviado (bug confirmado:
            // https://github.com/goauthentik/authentik/issues/22904). Removemos o parâmetro
            // para o logout completar; o usuário fica na tela de "desconectado" do Authentik
            // em vez de voltar automaticamente pro app.
            context.ProtocolMessage.PostLogoutRedirectUri = null;

            return Task.CompletedTask;
        }
    };
});

// Registrar serviços
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IAuthentikAdminService, AuthentikAdminService>();
builder.Services.AddHttpClient<IViaCepService, ViaCepService>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();

// Configurar Políticas de Autorização integradas ao Authentik
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
