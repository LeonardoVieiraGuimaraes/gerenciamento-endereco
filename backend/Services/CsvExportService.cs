using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using GerenciamentoEndereco.API.Models;

namespace GerenciamentoEndereco.API.Services;

public interface ICsvExportService
{
    byte[] ExportarEnderecosParaCsv(IEnumerable<Endereco> enderecos);
}

public class CsvExportService : ICsvExportService
{
    public byte[] ExportarEnderecosParaCsv(IEnumerable<Endereco> enderecos)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true
        };

        using var csv = new CsvWriter(writer, config);
        
        // Mapear apenas os campos desejados se necessário, ou exportar direto:
        csv.WriteRecords(enderecos.Select(e => new {
            e.Id,
            e.Cep,
            e.Logradouro,
            e.Numero,
            e.Complemento,
            e.Bairro,
            e.Cidade,
            e.Uf
        }));
        
        writer.Flush();
        return memoryStream.ToArray();
    }
}
