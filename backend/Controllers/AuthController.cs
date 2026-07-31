using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models.ViewModels;
using GerenciamentoEndereco.API.Services;
using System.Linq;

namespace GerenciamentoEndereco.API.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IAuthentikAdminService _authentikAdminService;

    public AuthController(AppDbContext context, IAuthentikAdminService authentikAdminService)
    {
        _context = context;
        _authentikAdminService = authentikAdminService;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        // Se o usuário já estiver logado, redireciona de volta
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return LocalRedirect(returnUrl);
        }

        // Caso contrário, dispara o desafio do OpenID Connect (redireciona para o Keycloak)
        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // O RP-Initiated Logout (redirect pro end-session do Authentik) está quebrado
        // no Authentik 2026.5.x (retorna "Bad Request" — bug confirmado upstream:
        // https://github.com/goauthentik/authentik/issues/22904). Em vez de depender
        // desse redirect, revogamos a sessão diretamente via Admin API antes de encerrar
        // a sessão local — assim o usuário sai de verdade (app + Authentik) sem passar
        // pela tela de erro.
        var username = User.FindFirst("preferred_username")?.Value ?? User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            await _authentikAdminService.RevokeAllSessionsAsync(username);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/");
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

        var viewModel = new PerfilViewModel
        {
            Nome = nome ?? "Usuário",
            Username = username ?? "-",
            Email = email ?? "-",
            Roles = roles,
            EnderecoCount = enderecoCount
        };

        return View(viewModel);
    }
}
