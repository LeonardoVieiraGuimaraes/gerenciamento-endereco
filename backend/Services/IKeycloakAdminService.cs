using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GerenciamentoEndereco.API.Services
{
    public class KeycloakUser
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("enabled")] public bool Enabled { get; set; }

        public string? Name => string.IsNullOrWhiteSpace($"{FirstName} {LastName}".Trim())
            ? null
            : $"{FirstName} {LastName}".Trim();
    }

    public class CreateKeycloakUserRequest
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class UpdateKeycloakUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool Enabled { get; set; }
        public string? NovaSenha { get; set; }
    }

    public interface IKeycloakAdminService
    {
        Task<List<KeycloakUser>> GetUsersAsync();
        Task<KeycloakUser?> GetUserAsync(string id);
        Task<bool> CreateUserAsync(CreateKeycloakUserRequest request);
        Task<bool> UpdateUserAsync(string id, UpdateKeycloakUserRequest request);
        Task<bool> DeleteUserAsync(string id);

        /// <summary>
        /// Revoga todas as sessões ativas do usuário no Keycloak (logout forçado).
        /// </summary>
        Task<bool> LogoutUserSessionsAsync(string id);

        /// <summary>
        /// Identificador da credencial de verificação em duas etapas (TOTP) do
        /// usuário, ou <c>null</c> se ele não tiver 2FA cadastrado.
        ///
        /// Serve para montar a ação de remoção: o Keycloak exige o id exato da
        /// credencial a excluir, e essa informação só existe do lado dele.
        /// </summary>
        Task<string?> ObterIdCredencial2FAAsync(string userId);
    }
}
