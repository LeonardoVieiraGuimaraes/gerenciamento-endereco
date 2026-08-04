using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models.ViewModels;
using GerenciamentoEndereco.API.Services;
using System.Linq;

namespace GerenciamentoEndereco.API.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IKeycloakAdminService _keycloakAdminService;

    public AuthController(AppDbContext context, IKeycloakAdminService keycloakAdminService)
    {
        _context = context;
        _keycloakAdminService = keycloakAdminService;
    }

    [HttpGet]
    [EnableRateLimiting("login")]
    public IActionResult Login(string returnUrl = "/")
    {
        // AuthenticationProperties.RedirectUri (usado pelo Challenge abaixo) não é
        // validado pelo framework como LocalRedirect() é — sem essa checagem, um
        // returnUrl apontando pra fora do site vira um open redirect logo após o
        // login (ex.: /Auth/Login?returnUrl=https://site-malicioso.com).
        if (!Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        // Se o usuário já estiver logado, redireciona de volta
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return LocalRedirect(returnUrl);
        }

        // Caso contrário, dispara o desafio do OpenID Connect (redireciona para o Keycloak)
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        // Sem [ValidateAntiForgeryToken] de propósito: esse token fica vinculado aos
        // claims do usuário no momento em que a página foi renderizada. Como o Keycloak
        // mantém uma sessão SSO ativa, é comum o usuário ser re-autenticado silenciosamente
        // entre um clique em "Sair" e outro (ex.: after voltar pra Home, ou usando o botão
        // voltar do navegador) — o que invalida esse token e gera um 400 feio numa ação que
        // não é destrutiva (o pior caso de um CSRF forçando logout é só deslogar a vítima).
        //
        // Diferente do Authentik, o RP-Initiated Logout do Keycloak funciona normalmente
        // (sem bug conhecido) — o próprio SignOut do OIDC já redireciona pro
        // end_session_endpoint e volta via post.logout.redirect.uris do client.
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    // Dispara uma ação nativa do Keycloak (trocar senha, configurar 2FA...) usando o
    // parâmetro kc_action — o usuário é levado pra tela de login do Keycloak (com o
    // NOSSO tema customizado, em pt-BR) já na etapa certa, sem precisar do Account
    // Console (app React separado, que exigiria configuração de CORS à parte).
    [Authorize]
    [HttpGet]
    public IActionResult AlterarSenha()
    {
        var props = new AuthenticationProperties { RedirectUri = "/Auth/Perfil" };
        props.Items["kc_action"] = "UPDATE_PASSWORD";
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet]
    public IActionResult ConfigurarDoisFatores()
    {
        var props = new AuthenticationProperties { RedirectUri = "/Auth/Perfil" };
        props.Items["kc_action"] = "CONFIGURE_TOTP";
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Remove a verificação em duas etapas da conta de quem está logado.
    ///
    /// Quem apaga é o próprio Keycloak, pela ação nativa "delete_credential": a
    /// aplicação apenas descobre o id da credencial e encaminha o usuário para a
    /// tela de confirmação. Assim a exclusão continua exigindo a sessão da
    /// pessoa — a aplicação nunca remove credencial de ninguém por conta própria.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverDoisFatores()
    {
        var keycloakId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(keycloakId))
        {
            TempData["ErrorMessage"] = "Não foi possível identificar sua conta.";
            return RedirectToAction(nameof(Perfil));
        }

        var credencialId = await _keycloakAdminService.ObterIdCredencial2FAAsync(keycloakId);
        if (string.IsNullOrEmpty(credencialId))
        {
            TempData["ErrorMessage"] = "Não há verificação em duas etapas cadastrada nesta conta.";
            return RedirectToAction(nameof(Perfil));
        }

        var props = new AuthenticationProperties { RedirectUri = "/Auth/Perfil" };
        props.Items["kc_action"] = $"delete_credential:{credencialId}";
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var username = User.FindFirst("preferred_username")?.Value
                    ?? User.Identity?.Name;

        var nome = User.FindFirst("name")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                ?? username;

        var email = User.FindFirst("email")?.Value
                 ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        var roles = User.Claims
            .Where(c => c.Type == "roles" || c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var enderecoCount = 0;
        if (!string.IsNullOrEmpty(username))
        {
            enderecoCount = await _context.Enderecos
                .Include(e => e.Usuario)
                .Where(e => e.Usuario.Username == username)
                .CountAsync();
        }

        // Só o Keycloak sabe quais credenciais a conta tem — o token não carrega
        // essa informação. Uma falha na consulta não deve derrubar o perfil: sem
        // resposta, a tela apenas oferece configurar o 2FA.
        var possuiDoisFatores = false;
        var keycloakId = User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(keycloakId))
        {
            possuiDoisFatores = await _keycloakAdminService.ObterIdCredencial2FAAsync(keycloakId) is not null;
        }

        var viewModel = new PerfilViewModel
        {
            Nome = nome ?? "Usuário",
            Username = username ?? "-",
            Email = email ?? "-",
            Roles = roles,
            EnderecoCount = enderecoCount,
            PossuiDoisFatores = possuiDoisFatores
        };

        return View(viewModel);
    }
}
