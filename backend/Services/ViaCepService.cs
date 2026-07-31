using System.Text.Json;
using GerenciamentoEndereco.API.Models;

namespace GerenciamentoEndereco.API.Services;

public interface IViaCepService
{
    Task<ViaCepResponse?> BuscarCepAsync(string cep);
}

public class ViaCepService : IViaCepService
{
    private readonly HttpClient _httpClient;

    public ViaCepService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://viacep.com.br/ws/");
    }

    public async Task<ViaCepResponse?> BuscarCepAsync(string cep)
    {
        // Limpa a formatação do CEP
        cep = cep.Replace("-", "").Replace(".", "").Trim();

        if (cep.Length != 8)
            return null;

        var response = await _httpClient.GetAsync($"{cep}/json/");
        
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var viaCepResult = JsonSerializer.Deserialize<ViaCepResponse>(json);

        if (viaCepResult != null && viaCepResult.Erro)
            return null;

        return viaCepResult;
    }
}
