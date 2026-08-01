using System.Security.Claims;
using FluentAssertions;
using GerenciamentoEndereco.API.Controllers;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Models.ViewModels;
using GerenciamentoEndereco.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoEndereco.Tests.Controllers;

/// <summary>
/// Foco destes testes: garantir o isolamento entre usuários. A API é o caminho
/// mais fácil de explorar — basta trocar o id na URL — então cada operação
/// precisa provar que não vaza nem altera dado de outra pessoa.
/// </summary>
public class EnderecosApiControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UsuarioLocalService _usuarios;

    private const string Dono = "leonardo";
    private const string Outro = "maria";

    public EnderecosApiControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _usuarios = new UsuarioLocalService(_context);
    }

    private EnderecosApiController CriarController(string username, bool admin = false)
    {
        var claims = new List<Claim> { new("preferred_username", username) };
        claims.Add(new Claim("roles", admin ? "ADMIN" : "USUARIO"));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, "TestAuth", "preferred_username", ClaimTypes.Role));

        return new EnderecosApiController(_context, _usuarios)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    /// <summary>Cria dois usuários com um endereço cada. Retorna (idDoDono, idDoOutro).</summary>
    private async Task<(int idDono, int idOutro)> SemearAsync()
    {
        var dono = new Usuario { Nome = "Leonardo", Username = Dono, Senha = "x" };
        var outro = new Usuario { Nome = "Maria", Username = Outro, Senha = "x" };
        _context.Usuarios.AddRange(dono, outro);
        await _context.SaveChangesAsync();

        var doDono = NovoEndereco("01001-000", dono.Id);
        var doOutro = NovoEndereco("20040-020", outro.Id);
        _context.Enderecos.AddRange(doDono, doOutro);
        await _context.SaveChangesAsync();

        return (doDono.Id, doOutro.Id);
    }

    private static Endereco NovoEndereco(string cep, int usuarioId) => new()
    {
        Cep = cep,
        Logradouro = "Rua Teste",
        Bairro = "Centro",
        Cidade = "São Paulo",
        Uf = "SP",
        Numero = "100",
        UsuarioId = usuarioId
    };

    private static EnderecoRequest Requisicao(string cep = "30130-110") => new()
    {
        Cep = cep,
        Logradouro = "Avenida Nova",
        Bairro = "Centro",
        Cidade = "Belo Horizonte",
        Uf = "MG",
        Numero = "500"
    };

    // ---------- Listagem ----------

    [Fact]
    public async Task GetEnderecos_UsuarioComum_DeveVerApenasOsProprios()
    {
        await SemearAsync();

        var resultado = await CriarController(Dono).GetEnderecos();

        resultado.Value.Should().HaveCount(1);
        resultado.Value!.Single().Cep.Should().Be("01001-000");
    }

    [Fact]
    public async Task GetEnderecos_Admin_DeveVerTodos()
    {
        await SemearAsync();

        var resultado = await CriarController(Dono, admin: true).GetEnderecos();

        resultado.Value.Should().HaveCount(2);
    }

    // ---------- Consulta por id ----------

    [Fact]
    public async Task GetEndereco_DoProprioUsuario_DeveRetornar()
    {
        var (idDono, _) = await SemearAsync();

        var resultado = await CriarController(Dono).GetEndereco(idDono);

        resultado.Value.Should().NotBeNull();
        resultado.Value!.Cep.Should().Be("01001-000");
    }

    [Fact]
    public async Task GetEndereco_DeOutroUsuario_DeveNegar()
    {
        var (_, idOutro) = await SemearAsync();

        var resultado = await CriarController(Dono).GetEndereco(idOutro);

        resultado.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetEndereco_Inexistente_DeveRetornarNaoEncontrado()
    {
        await SemearAsync();

        var resultado = await CriarController(Dono).GetEndereco(9999);

        resultado.Result.Should().BeOfType<NotFoundResult>();
    }

    // ---------- Criação ----------

    [Fact]
    public async Task PostEndereco_DeveCriarVinculadoAoUsuarioAutenticado()
    {
        await SemearAsync();

        var resultado = await CriarController(Dono).PostEndereco(Requisicao());

        var criado = resultado.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var endereco = criado.Value.Should().BeOfType<Endereco>().Subject;

        var dono = await _context.Usuarios.FirstAsync(u => u.Username == Dono);
        endereco.UsuarioId.Should().Be(dono.Id);
        endereco.Cep.Should().Be("30130-110");
    }

    [Fact]
    public async Task PostEndereco_UsuarioSemRegistroLocal_DeveCriarUsuarioEEndereco()
    {
        var resultado = await CriarController("recem-chegado").PostEndereco(Requisicao());

        resultado.Result.Should().BeOfType<CreatedAtActionResult>();
        (await _context.Usuarios.AnyAsync(u => u.Username == "recem-chegado")).Should().BeTrue();
    }

    // ---------- Atualização ----------

    [Fact]
    public async Task PutEndereco_DoProprioUsuario_DeveAtualizar()
    {
        var (idDono, _) = await SemearAsync();

        var resultado = await CriarController(Dono).PutEndereco(idDono, Requisicao("99999-999"));

        resultado.Should().BeOfType<NoContentResult>();
        (await _context.Enderecos.FindAsync(idDono))!.Cep.Should().Be("99999-999");
    }

    [Fact]
    public async Task PutEndereco_DeOutroUsuario_DeveNegarESemAlterar()
    {
        var (_, idOutro) = await SemearAsync();

        var resultado = await CriarController(Dono).PutEndereco(idOutro, Requisicao("99999-999"));

        resultado.Should().BeOfType<ForbidResult>();
        (await _context.Enderecos.FindAsync(idOutro))!.Cep.Should().Be("20040-020");
    }

    [Fact]
    public async Task PutEndereco_NaoDeveTransferirODono()
    {
        var (idDono, _) = await SemearAsync();
        var donoOriginal = (await _context.Enderecos.AsNoTracking().FirstAsync(e => e.Id == idDono)).UsuarioId;

        await CriarController(Dono).PutEndereco(idDono, Requisicao());

        (await _context.Enderecos.FindAsync(idDono))!.UsuarioId.Should().Be(donoOriginal);
    }

    [Fact]
    public async Task PutEndereco_Admin_PodeAtualizarDeQualquerUsuario()
    {
        var (_, idOutro) = await SemearAsync();

        var resultado = await CriarController(Dono, admin: true).PutEndereco(idOutro, Requisicao("11111-111"));

        resultado.Should().BeOfType<NoContentResult>();
        (await _context.Enderecos.FindAsync(idOutro))!.Cep.Should().Be("11111-111");
    }

    // ---------- Exclusão ----------

    [Fact]
    public async Task DeleteEndereco_DoProprioUsuario_DeveExcluir()
    {
        var (idDono, _) = await SemearAsync();

        var resultado = await CriarController(Dono).DeleteEndereco(idDono);

        resultado.Should().BeOfType<NoContentResult>();
        (await _context.Enderecos.FindAsync(idDono)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteEndereco_DeOutroUsuario_DeveNegarESemExcluir()
    {
        var (_, idOutro) = await SemearAsync();

        var resultado = await CriarController(Dono).DeleteEndereco(idOutro);

        resultado.Should().BeOfType<ForbidResult>();
        (await _context.Enderecos.FindAsync(idOutro)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteEndereco_Inexistente_DeveRetornarNaoEncontrado()
    {
        await SemearAsync();

        var resultado = await CriarController(Dono).DeleteEndereco(9999);

        resultado.Should().BeOfType<NotFoundResult>();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
