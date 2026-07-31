using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GerenciamentoEndereco.API.Services
{
    public class AuthentikUser
    {
        [JsonPropertyName("pk")] public int Pk { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("is_active")] public bool IsActive { get; set; }
        [JsonPropertyName("is_superuser")] public bool IsSuperuser { get; set; }
    }

    public class CreateAuthentikUserRequest
    {
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class UpdateAuthentikUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public string? NovaSenha { get; set; }
    }

    public interface IAuthentikAdminService
    {
        Task<List<AuthentikUser>> GetUsersAsync();
        Task<AuthentikUser?> GetUserAsync(int pk);
        Task<bool> CreateUserAsync(CreateAuthentikUserRequest request);
        Task<bool> UpdateUserAsync(int pk, UpdateAuthentikUserRequest request);
        Task<bool> DeleteUserAsync(int pk);

        /// <summary>
        /// Revoga todas as sessões ativas do usuário no Authentik. Usado no logout do app,
        /// já que o end-session (RP-Initiated Logout) do Authentik 2026.5.x tem um bug
        /// conhecido que rejeita o redirect com "Bad Request".
        /// </summary>
        Task<int> RevokeAllSessionsAsync(string username);
    }
}
