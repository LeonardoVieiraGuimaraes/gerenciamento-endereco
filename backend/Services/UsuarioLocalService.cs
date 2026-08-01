using System.Security.Claims;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoEndereco.API.Services;

/// <summary>
/// Resolve o usuário local correspondente a quem está autenticado no Keycloak.
///
/// A aplicação não guarda credenciais: quem manda na identidade é o Keycloak.
/// Mas os endereços precisam de uma chave estrangeira local, então mantemos um
/// registro espelho em Usuarios, criado na primeira vez que a pessoa acessa.
/// </summary>
public interface IUsuarioLocalService
{
    /// <summary>Nome de usuário (preferred_username) de quem está autenticado.</summary>
    string? ObterUsername(ClaimsPrincipal principal);

    /// <summary>Identificador imutável do usuário no Keycloak (claim "sub").</summary>
    string? ObterKeycloakId(ClaimsPrincipal principal);

    /// <summary>Indica se o usuário autenticado tem perfil de administrador.</summary>
    bool EhAdmin(ClaimsPrincipal principal);

    /// <summary>
    /// Retorna o usuário local, criando-o se ainda não existir.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// Quando não é possível identificar o usuário a partir das claims.
    /// </exception>
    Task<Usuario> ObterOuCriarAsync(ClaimsPrincipal principal);
}

public class UsuarioLocalService : IUsuarioLocalService
{
    private readonly AppDbContext _context;

    public UsuarioLocalService(AppDbContext context)
    {
        _context = context;
    }

    // ClaimTypes.NameIdentifier foi retirado desta lista de propósito: ele carrega
    // o "sub" (um GUID), não um nome de usuário. Se as claims de nome faltassem,
    // o GUID acabaria gravado na coluna Username.
    public string? ObterUsername(ClaimsPrincipal principal) =>
        principal.FindFirstValue("preferred_username")
        ?? principal.FindFirstValue(ClaimTypes.Name)
        ?? principal.Identity?.Name;

    // O .NET mapeia "sub" para ClaimTypes.NameIdentifier por padrão, mas a claim
    // crua pode aparecer quando o mapeamento está desligado — por isso as duas.
    public string? ObterKeycloakId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue("sub");

    // As roles do Keycloak vêm em maiúsculas ("ADMIN"). A comparação precisa
    // ignorar caixa: uma versão anterior comparava com "admin" minúsculo e
    // nunca batia, fazendo o admin ser tratado como usuário comum.
    public bool EhAdmin(ClaimsPrincipal principal) =>
        principal.IsInRole("ADMIN") ||
        principal.HasClaim(c => c.Type == "roles" &&
                                string.Equals(c.Value, "admin", StringComparison.OrdinalIgnoreCase)) ||
        principal.HasClaim(c => c.Type == "client_role" && c.Value == "usuarios.manage");

    /// <summary>
    /// Nome de exibição vindo do Keycloak. Cai para o nome de usuário quando a
    /// conta não tem nome preenchido.
    /// </summary>
    private static string ObterNome(ClaimsPrincipal principal, string username) =>
        principal.FindFirstValue("name")
        ?? principal.FindFirstValue(ClaimTypes.GivenName)
        ?? username;

    public async Task<Usuario> ObterOuCriarAsync(ClaimsPrincipal principal)
    {
        var username = ObterUsername(principal);
        var keycloakId = ObterKeycloakId(principal);

        if (string.IsNullOrEmpty(username))
            throw new UnauthorizedAccessException("Usuário não identificado no sistema de autenticação.");

        // 1) Caminho normal: casar pelo identificador imutável do Keycloak.
        if (!string.IsNullOrEmpty(keycloakId))
        {
            var porId = await _context.Usuarios.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
            if (porId != null)
            {
                // Nome e nome de usuário podem mudar no Keycloak sem deixar de ser
                // a mesma pessoa. Como a cópia local é usada para exibir e filtrar
                // (tela de endereços do admin), ela precisa acompanhar — senão
                // continuaria mostrando o dado antigo indefinidamente.
                var nomeAtual = ObterNome(principal, username);

                if (porId.Username != username || porId.Nome != nomeAtual)
                {
                    porId.Username = username;
                    porId.Nome = nomeAtual;
                    await _context.SaveChangesAsync();
                }
                return porId;
            }
        }

        // 2) Registro criado antes desta coluna existir: casa pelo nome de usuário,
        //    mas SOMENTE se ainda não pertencer a ninguém (KeycloakId nulo).
        //    Essa restrição é o que impede o vazamento: um registro já vinculado a
        //    um "sub" nunca é entregue a outro, mesmo com o mesmo nome de usuário.
        var legado = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == username && u.KeycloakId == null);

        if (legado != null)
        {
            legado.KeycloakId = keycloakId;
            await _context.SaveChangesAsync();
            return legado;
        }

        // "Senha" existe por causa do schema pedido no enunciado do teste, mas não
        // guarda credencial: a autenticação é sempre feita pelo Keycloak.
        var usuario = new Usuario
        {
            Username = username,
            KeycloakId = keycloakId,
            Nome = ObterNome(principal, username),
            Senha = "KEYCLOAK_MANAGED"
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return usuario;
    }
}
