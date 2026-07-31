using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GerenciamentoEndereco.API.Services
{
    // Chama a Admin REST API do Keycloak (/admin/realms/{realm}/...) autenticando via
    // client_credentials (client de service account "backend-admin-api"), independente
    // do usuário logado. O token de admin é de curta duração — cacheamos em memória do
    // processo até perto de expirar.
    public class KeycloakAdminService : IKeycloakAdminService
    {
        private static string? _cachedToken;
        private static DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
        private static readonly SemaphoreSlim TokenLock = new(1, 1);

        private readonly HttpClient _httpClient;
        private readonly ILogger<KeycloakAdminService> _logger;
        private readonly string _realm;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public KeycloakAdminService(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakAdminService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["KeycloakAdmin:BaseUrl"] ?? "http://keycloak:8080";
            _httpClient.BaseAddress = new Uri(baseUrl);

            _realm = configuration["KeycloakAdmin:Realm"] ?? "gerenciamento-endereco";
            _clientId = configuration["KeycloakAdmin:ClientId"] ?? "backend-admin-api";
            _clientSecret = configuration["KeycloakAdmin:ClientSecret"] ?? "";
        }

        private async Task<string?> GetAccessTokenAsync()
        {
            if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            await TokenLock.WaitAsync();
            try
            {
                if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiresAt)
                    return _cachedToken;

                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret
                });

                var response = await _httpClient.PostAsync($"/realms/{_realm}/protocol/openid-connect/token", form);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao obter token de admin do Keycloak: {Status} - {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                    return null;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var token = doc.RootElement.GetProperty("access_token").GetString();
                var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

                _cachedToken = token;
                _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(expiresIn - 15, 5));
                return token;
            }
            finally
            {
                TokenLock.Release();
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
        {
            var token = await GetAccessTokenAsync();
            using var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            if (body != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            }
            return await _httpClient.SendAsync(request);
        }

        public async Task<List<KeycloakUser>> GetUsersAsync()
        {
            var response = await SendAsync(HttpMethod.Get, $"/admin/realms/{_realm}/users?max=500");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao buscar usuários no Keycloak: {Status}", response.StatusCode);
                return new List<KeycloakUser>();
            }

            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<KeycloakUser>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }

        public async Task<KeycloakUser?> GetUserAsync(string id)
        {
            var response = await SendAsync(HttpMethod.Get, $"/admin/realms/{_realm}/users/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<KeycloakUser>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> CreateUserAsync(CreateKeycloakUserRequest request)
        {
            var payload = new
            {
                username = request.Username,
                firstName = request.FirstName,
                lastName = request.LastName,
                email = request.Email,
                enabled = true,
                emailVerified = true,
                credentials = string.IsNullOrEmpty(request.Password)
                    ? null
                    : new[] { new { type = "password", value = request.Password, temporary = false } }
            };

            var response = await SendAsync(HttpMethod.Post, $"/admin/realms/{_realm}/users", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao criar usuário no Keycloak: {Status} - {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateUserAsync(string id, UpdateKeycloakUserRequest request)
        {
            var payload = new
            {
                firstName = request.FirstName,
                lastName = request.LastName,
                email = request.Email,
                enabled = request.Enabled
            };

            var response = await SendAsync(HttpMethod.Put, $"/admin/realms/{_realm}/users/{id}", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao atualizar usuário {Id} no Keycloak: {Status} - {Body}", id, response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }

            if (!string.IsNullOrEmpty(request.NovaSenha))
            {
                var pwdResponse = await SendAsync(HttpMethod.Put, $"/admin/realms/{_realm}/users/{id}/reset-password",
                    new { type = "password", value = request.NovaSenha, temporary = false });

                if (!pwdResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro ao redefinir senha do usuário {Id} no Keycloak: {Status}", id, pwdResponse.StatusCode);
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var response = await SendAsync(HttpMethod.Delete, $"/admin/realms/{_realm}/users/{id}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao excluir usuário {Id} no Keycloak: {Status}", id, response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> LogoutUserSessionsAsync(string id)
        {
            var response = await SendAsync(HttpMethod.Post, $"/admin/realms/{_realm}/users/{id}/logout");
            return response.IsSuccessStatusCode;
        }
    }
}
