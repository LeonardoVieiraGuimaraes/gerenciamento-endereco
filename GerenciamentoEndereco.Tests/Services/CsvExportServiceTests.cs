using System.Text;
using FluentAssertions;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Services;

namespace GerenciamentoEndereco.Tests.Services;

public class CsvExportServiceTests
{
    private readonly CsvExportService _sut;

    public CsvExportServiceTests()
    {
        _sut = new CsvExportService();
    }

    [Fact]
    public void ExportarEnderecosParaCsv_ListaValida_RetornaCsvEmBytes()
    {
        // Arrange
        var enderecos = new List<Endereco>
        {
            new Endereco
            {
                Id = 1,
                Cep = "01001-000",
                Logradouro = "Praça da Sé",
                Numero = "s/n",
                Complemento = "lado ímpar",
                Bairro = "Sé",
                Cidade = "São Paulo",
                Uf = "SP"
            },
            new Endereco
            {
                Id = 2,
                Cep = "20031-900",
                Logradouro = "Avenida República do Chile",
                Numero = "65",
                Complemento = "",
                Bairro = "Centro",
                Cidade = "Rio de Janeiro",
                Uf = "RJ"
            }
        };

        // Act
        var result = _sut.ExportarEnderecosParaCsv(enderecos);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        var csvString = Encoding.UTF8.GetString(result).Replace("\uFEFF", "");
        
        // Verifica cabeçalhos
        csvString.Should().Contain("Id,Cep,Logradouro,Numero,Complemento,Bairro,Cidade,Uf");
        
        // Verifica primeiro registro
        csvString.Should().Contain("1,01001-000,Praça da Sé,s/n,lado ímpar,Sé,São Paulo,SP");
        
        // Verifica segundo registro
        csvString.Should().Contain("2,20031-900,Avenida República do Chile,65,,Centro,Rio de Janeiro,RJ");
    }

    [Fact]
    public void ExportarEnderecosParaCsv_ListaVazia_RetornaApenasCabecalho()
    {
        // Arrange
        var enderecos = new List<Endereco>();

        // Act
        var result = _sut.ExportarEnderecosParaCsv(enderecos);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        var csvString = Encoding.UTF8.GetString(result).Replace("\uFEFF", "");
        
        var lines = csvString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().Be(1); // Apenas o cabeçalho
        lines[0].Should().Be("Id,Cep,Logradouro,Numero,Complemento,Bairro,Cidade,Uf");
    }
}
