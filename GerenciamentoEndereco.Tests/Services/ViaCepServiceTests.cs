using System.Net;
using System.Text.Json;
using FluentAssertions;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Services;
using Moq;
using Moq.Protected;

namespace GerenciamentoEndereco.Tests.Services;

public class ViaCepServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ViaCepService _sut; // System Under Test

    public ViaCepServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://viacep.com.br/ws/")
        };
        _sut = new ViaCepService(_httpClient);
    }

    [Fact]
    public async Task BuscarCepAsync_CepValido_RetornaViaCepResponse()
    {
        // Arrange
        var cep = "01001000";
        var expectedResponse = new ViaCepResponse
        {
            Cep = "01001-000",
            Logradouro = "Praça da Sé",
            Complemento = "lado ímpar",
            Bairro = "Sé",
            Localidade = "São Paulo",
            Uf = "SP",
            Erro = false
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _sut.BuscarCepAsync(cep);

        // Assert
        result.Should().NotBeNull();
        result!.Cep.Should().Be("01001-000");
        result.Logradouro.Should().Be("Praça da Sé");
        result.Bairro.Should().Be("Sé");
        result.Localidade.Should().Be("São Paulo");
        result.Uf.Should().Be("SP");
    }

    [Fact]
    public async Task BuscarCepAsync_CepComMascara_RetornaViaCepResponse()
    {
        // Arrange
        var cep = "01001-000"; // Com máscara
        var expectedResponse = new ViaCepResponse
        {
            Cep = "01001-000",
            Erro = false
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "https://viacep.com.br/ws/01001000/json/"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _sut.BuscarCepAsync(cep);

        // Assert
        result.Should().NotBeNull();
        result!.Cep.Should().Be("01001-000");
    }

    [Fact]
    public async Task BuscarCepAsync_CepInvalidoTamanho_RetornaNull()
    {
        // Arrange
        var cep = "123"; // Tamanho incorreto

        // Act
        var result = await _sut.BuscarCepAsync(cep);

        // Assert
        result.Should().BeNull();
        // Garante que o HttpClient não foi chamado
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task BuscarCepAsync_CepNaoEncontrado_RetornaNull()
    {
        // Arrange
        var cep = "99999999";
        var expectedResponse = new ViaCepResponse
        {
            Erro = true
        };

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _sut.BuscarCepAsync(cep);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task BuscarCepAsync_ApiViaCepForaDoAr_RetornaNull()
    {
        // Arrange
        var cep = "01001000";

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _sut.BuscarCepAsync(cep);

        // Assert
        result.Should().BeNull();
    }
}
