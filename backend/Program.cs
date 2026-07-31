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
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

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
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // SameAsRequest em Development pra não quebrar o teste local sobre HTTP puro;
    // em qualquer outro ambiente a app já roda atrás de HTTPS (direto ou via proxy
    // reverso, com ForwardedHeaders configurado abaixo), então força Secure sempre.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
})
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
    // Em produção o host externo é servido via HTTPS (TLS terminado no Cloudflare
    // Tunnel/proxy reverso); localmente é HTTP puro. O host interno (container-a-
    // container, dentro da rede Docker) é sempre HTTP.
    var externalScheme = builder.Configuration["Keycloak:ExternalScheme"] ?? "http";
    var externalRealmUrl = $"{externalScheme}://{builder.Configuration["Keycloak:ExternalHost"] ?? "localhost:8089"}/realms/gerenciamento-endereco";
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
    // diferentes; num deploy novo em produção, o Keycloak pode levar bem mais tempo pra
    // subir, já que a imagem é construída e o realm é importado do zero).
    //
    // Esse bloco roda de forma "lazy" (só na primeira requisição que toca autenticação —
    // inclusive /health, já que UseAuthentication roda antes de QUALQUER endpoint). Se a
    // última tentativa também falhar e a exceção não for capturada, ela fica "presa" no
    // cache do OptionsMonitor e volta a estourar em TODA requisição seguinte, mesmo
    // depois do Keycloak ficar disponível — por isso o catch aqui precisa cobrir
    // TODAS as tentativas, inclusive a última: melhor a app subir sem as chaves de
    // assinatura (login vai falhar até o Keycloak responder) do que ficar 500 pra sempre.
    using (var jwksClient = new HttpClient())
    {
        const int maxAttempts = 30;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
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
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    Log.Error(ex, "Não foi possível obter as chaves JWKS do Keycloak após {MaxAttempts} tentativas — a app vai subir mesmo assim, mas login/validação de token vão falhar até o Keycloak responder.", maxAttempts);
                    break;
                }
                Thread.Sleep(3000);
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
        // O Keycloak inclui o client_id como audience (aud/azp) do ID token — validar
        // isso garante que um token emitido pra OUTRO client do mesmo realm (ex.:
        // backend-admin-api) não seja aceito aqui.
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Keycloak:ClientId"],
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

            // Repassa a ação nativa do Keycloak (trocar senha, configurar 2FA...) definida
            // em AuthController.AlterarSenha/ConfigurarDoisFatores.
            if (context.Properties.Items.TryGetValue("kc_action", out var kcAction) && !string.IsNullOrEmpty(kcAction))
            {
                context.ProtocolMessage.SetParameter("kc_action", kcAction);
            }

            return Task.CompletedTask;
        }
    };
});

// Registrar serviços
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>();
builder.Services.AddHttpClient<IViaCepService, ViaCepService>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddHealthChecks();

// Configurar Políticas de Autorização integradas ao Keycloak
// Nota: as políticas abaixo NÃO caem mais num catch-all "usuário autenticado passa",
// como faziam antes — isso tornava a checagem de role decorativa (qualquer usuário
// logado, mesmo sem role nenhuma, tinha acesso de leitura/escrita/exclusão).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EnderecoRead", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.read")
    ));

    options.AddPolicy("EnderecoWrite", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.write")
    ));

    options.AddPolicy("EnderecoDelete", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.delete")
    ));

    options.AddPolicy("EnderecoExport", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("ADMIN") ||
        context.User.IsInRole("USUARIO") ||
        context.User.HasClaim(c => c.Type == "roles" && (c.Value == "ADMIN" || c.Value == "USUARIO" || c.Value == "admin" || c.Value == "usuario")) ||
        context.User.HasClaim(c => c.Type == "client_role" && c.Value == "enderecos.export")
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

// Forwarded headers: necessário quando a app roda atrás de um reverse proxy (nginx,
// Traefik, Cloudflare Tunnel etc.) em produção, pra que UseHttpsRedirection/UseHsts e
// o Cookie.SecurePolicy="Always" enxerguem o esquema (https) e IP reais do cliente, e
// não os do proxy. Limpar KnownNetworks/KnownProxies é seguro aqui porque o próprio
// reverse proxy — não este app — é responsável por descartar cabeçalhos
// X-Forwarded-* vindos direto do cliente antes de repassar a requisição.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate limiting: protege contra abuso/automação em toda a aplicação, com políticas
// mais restritas nos pontos mais sensíveis (login e integração externa via CEP).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // BuscarCep chama a API pública do ViaCEP — sem limite próprio, um uso abusivo
    // (script disparando muito rápido) arrisca nosso IP ser banido/limitado por eles.
    options.AddPolicy("cep-lookup", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            }));

    // Mitiga automação repetida contra o início do fluxo OIDC. O brute-force de senha
    // em si já é protegido nativamente pelo Keycloak (Realm Settings > Security Defenses).
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

// Aplicar Migrations automaticamente ao iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
// Precisa ser o primeiro middleware: reescreve Scheme/RemoteIp a partir dos cabeçalhos
// X-Forwarded-* ANTES de qualquer coisa que dependa deles (HTTPS redirect, HSTS,
// cookie Secure, rate limiter por IP).
app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net data:; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";
    await next();
});

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
    // Depois de UseAuthentication/UseAuthorization pra que a política "cep-lookup"
    // (particionada por usuário autenticado) enxergue o HttpContext.User já populado.
    app.UseRateLimiter();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapHealthChecks("/health");

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
