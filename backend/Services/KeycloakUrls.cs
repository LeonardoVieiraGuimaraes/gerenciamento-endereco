namespace GerenciamentoEndereco.API.Services;

/// <summary>
/// Ponto único de montagem dos endereços do Keycloak.
///
/// Existe porque a mesma URL era construída em lugares diferentes (configuração
/// do OIDC e links das telas), cada um com seu próprio valor padrão. Bastou um
/// deles ficar apontando para "localhost" para o botão do Keycloak abrir o
/// servidor da máquina de quem clicava, em vez do ambiente publicado.
///
/// Regra: nenhuma tela ou serviço monta endereço do Keycloak por conta própria.
/// </summary>
public class KeycloakUrls
{
    /// <summary>
    /// Endereço que o NAVEGADOR do usuário enxerga (login, logout, console).
    /// Ex.: https://auth-enderecos.leoproti.com.br
    /// </summary>
    public string PublicoBase { get; }

    /// <summary>
    /// Endereço que a APLICAÇÃO usa para falar com o Keycloak dentro da rede
    /// Docker (token, userinfo, chaves). Ex.: http://keycloak:8080
    ///
    /// É diferente do público de propósito: o Keycloak responde num hostname que
    /// o navegador não resolve, e vice-versa.
    /// </summary>
    public string InternoBase { get; }

    /// <summary>Nome do realm usado pela aplicação.</summary>
    public string Realm { get; }

    public KeycloakUrls(IConfiguration configuration)
    {
        var esquema = configuration["Keycloak:ExternalScheme"] ?? "http";
        var hostPublico = configuration["Keycloak:ExternalHost"] ?? "localhost:8089";
        var hostInterno = configuration["Keycloak:InternalHost"] ?? "keycloak:8080";

        Realm = configuration["KeycloakAdmin:Realm"] ?? "gerenciamento-endereco";
        PublicoBase = $"{esquema}://{hostPublico}";
        InternoBase = $"http://{hostInterno}";
    }

    /// <summary>URL do realm vista pelo navegador (emissor do token).</summary>
    public string RealmPublico => $"{PublicoBase}/realms/{Realm}";

    /// <summary>URL do realm usada nas chamadas servidor-a-servidor.</summary>
    public string RealmInterno => $"{InternoBase}/realms/{Realm}";

    /// <summary>Console de administração do realm da aplicação.</summary>
    public string ConsoleAdmin => $"{PublicoBase}/admin/{Realm}/console/";

    /// <summary>
    /// Tela de um usuário específico dentro do console. Sem id, devolve o
    /// console — melhor levar à lista do que montar um link quebrado.
    /// </summary>
    public string ConsoleUsuario(string? id) =>
        string.IsNullOrEmpty(id)
            ? ConsoleAdmin
            : $"{ConsoleAdmin}#/{Realm}/users/{id}/settings";
}
