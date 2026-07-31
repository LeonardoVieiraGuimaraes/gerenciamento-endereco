using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GerenciamentoEndereco.API.Services
{
    // Chama a Admin API do Authentik (/api/v3/core/...) usando um token de serviço
    // fixo (criado via blueprint), em vez do access_token OIDC do usuário logado —
    // assim a gestão de usuários funciona independentemente de quem está logado.
    public class AuthentikAdminService : IAuthentikAdminService
    {
        private class UserListResponse
        {
            [JsonPropertyName("results")] public List<AuthentikUser> Results { get; set; } = new();
        }

        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthentikAdminService> _logger;
        private readonly string _defaultGroupName;

        public AuthentikAdminService(HttpClient httpClient, IConfiguration configuration, ILogger<AuthentikAdminService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["Authentik:BaseUrl"] ?? "http://authentik-server:9000";
            if (!baseUrl.StartsWith("http")) baseUrl = "http://" + baseUrl;
            _httpClient.BaseAddress = new Uri(baseUrl);

            var apiToken = configuration["Authentik:ApiToken"];
            if (!string.IsNullOrEmpty(apiToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            }

            _defaultGroupName = configuration["Authentik:DefaultGroupName"] ?? "USUARIO";
        }

        // HttpClient.PatchAsJsonAsync/PostAsJsonAsync (System.Net.Http.Json) demonstraram
        // retornar 200 sem persistir a alteração neste ambiente — usamos HttpRequestMessage
        // explícito, que funciona de forma confiável.
        private async Task<HttpResponseMessage> SendJsonAsync(HttpMethod method, string url, object body)
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            return await _httpClient.SendAsync(request);
        }

        public async Task<List<AuthentikUser>> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync("/api/v3/core/users/?page_size=500");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao buscar usuários no Authentik: {Status}", response.StatusCode);
                return new List<AuthentikUser>();
            }

            var payload = await response.Content.ReadFromJsonAsync<UserListResponse>();
            return payload?.Results ?? new List<AuthentikUser>();
        }

        public async Task<AuthentikUser?> GetUserAsync(int pk)
        {
            var response = await _httpClient.GetAsync($"/api/v3/core/users/{pk}/");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<AuthentikUser>();
        }

        private async Task<string?> ResolveDefaultGroupPkAsync()
        {
            var response = await _httpClient.GetAsync($"/api/v3/core/groups/?name={Uri.EscapeDataString(_defaultGroupName)}");
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var results = doc.RootElement.GetProperty("results");
            return results.GetArrayLength() > 0 ? results[0].GetProperty("pk").GetString() : null;
        }

        public async Task<bool> CreateUserAsync(CreateAuthentikUserRequest request)
        {
            var groupPk = await ResolveDefaultGroupPkAsync();
            var payload = new
            {
                username = request.Username,
                name = request.Name,
                email = request.Email,
                is_active = true,
                groups = groupPk != null ? new[] { groupPk } : Array.Empty<string>()
            };

            var response = await SendJsonAsync(HttpMethod.Post, "/api/v3/core/users/", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao criar usuário no Authentik: {Status} - {Body}", response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }

            var created = await response.Content.ReadFromJsonAsync<AuthentikUser>();
            if (created == null) return false;
            if (string.IsNullOrEmpty(request.Password)) return true;

            return await SetPasswordAsync(created.Pk, request.Password);
        }

        private async Task<bool> SetPasswordAsync(int pk, string password)
        {
            var response = await SendJsonAsync(HttpMethod.Post, $"/api/v3/core/users/{pk}/set_password/", new { password });
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao definir senha do usuário {Pk} no Authentik: {Status}", pk, response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(int pk, UpdateAuthentikUserRequest request)
        {
            var payload = new { name = request.Name, email = request.Email, is_active = request.IsActive };
            var response = await SendJsonAsync(HttpMethod.Patch, $"/api/v3/core/users/{pk}/", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao atualizar usuário {Pk} no Authentik: {Status} - {Body}", pk, response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }

            if (!string.IsNullOrEmpty(request.NovaSenha))
            {
                return await SetPasswordAsync(pk, request.NovaSenha);
            }

            return true;
        }

        public async Task<bool> DeleteUserAsync(int pk)
        {
            var response = await _httpClient.DeleteAsync($"/api/v3/core/users/{pk}/");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao excluir usuário {Pk} no Authentik: {Status}", pk, response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<int> RevokeAllSessionsAsync(string username)
        {
            var response = await _httpClient.GetAsync($"/api/v3/core/authenticated_sessions/?user__username={Uri.EscapeDataString(username)}");
            if (!response.IsSuccessStatusCode) return 0;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var results = doc.RootElement.GetProperty("results");

            var revoked = 0;
            foreach (var session in results.EnumerateArray())
            {
                var uuid = session.GetProperty("uuid").GetString();
                var del = await _httpClient.DeleteAsync($"/api/v3/core/authenticated_sessions/{uuid}/");
                if (del.IsSuccessStatusCode) revoked++;
            }

            return revoked;
        }
    }
}
