using System.Security.Claims;
using FluentAssertions;
using GerenciamentoEndereco.API.Controllers;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GerenciamentoEndereco.Tests.Controllers;

public class EnderecosControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICsvExportService> _csvExportServiceMock;
    private readonly Mock<IViaCepService> _viaCepServiceMock;
    private readonly Mock<IUsuarioLocalService> _usuarioLocalServiceMock;
    private readonly EnderecosController _controller;
    private readonly string _testUsername = "testuser";

    public EnderecosControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _csvExportServiceMock = new Mock<ICsvExportService>();
        _viaCepServiceMock = new Mock<IViaCepService>();

        // Usuário comum por padrão: os testes existentes verificam justamente que
        // cada um enxerga só os próprios endereços. Um teste específico troca o
        // EhAdmin para true quando precisa do comportamento de administrador.
        _usuarioLocalServiceMock = new Mock<IUsuarioLocalService>();
        _usuarioLocalServiceMock.Setup(s => s.EhAdmin(It.IsAny<ClaimsPrincipal>())).Returns(false);
        _usuarioLocalServiceMock.Setup(s => s.ObterUsername(It.IsAny<ClaimsPrincipal>())).Returns(_testUsername);

        _controller = new EnderecosController(
            _context,
            _csvExportServiceMock.Object,
            _viaCepServiceMock.Object,
            _usuarioLocalServiceMock.Object);

        // Simula usuário autenticado no HttpContext
        var claims = new List<Claim>
        {
            new Claim("preferred_username", _testUsername),
            new Claim("name", "Test User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Index_RetornaViewResultComEnderecosDoUsuario()
    {
        // Arrange
        var user = new Usuario { Username = _testUsername, Nome = "Test User", Senha = "123" };
        _context.Usuarios.Add(user);
        
        var outroUser = new Usuario { Username = "outro", Nome = "Outro", Senha = "123" };
        _context.Usuarios.Add(outroUser);

        _context.Enderecos.Add(new Endereco { Cep = "111", Logradouro = "Rua 1", Usuario = user });
        _context.Enderecos.Add(new Endereco { Cep = "222", Logradouro = "Rua 2", Usuario = user });
        _context.Enderecos.Add(new Endereco { Cep = "333", Logradouro = "Rua 3", Usuario = outroUser });
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<Endereco>>().Subject;
        
        model.Should().HaveCount(2);
        model.Select(e => e.Logradouro).Should().Contain("Rua 1", "Rua 2");
    }

    [Fact]
    public async Task Create_PostValido_CriaEnderecoERedirecionaParaIndex()
    {
        // Arrange
        var endereco = new Endereco
        {
            Cep = "01001-000",
            Logradouro = "Praça da Sé",
            Numero = "1",
            Bairro = "Sé",
            Cidade = "São Paulo",
            Uf = "SP"
        };

        // Act
        var result = await _controller.Create(endereco);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");

        var enderecosNoBanco = await _context.Enderecos.ToListAsync();
        enderecosNoBanco.Should().HaveCount(1);
        enderecosNoBanco.First().Logradouro.Should().Be("Praça da Sé");
        
        // Verifica se o usuário local foi criado por GetOrCreateLocalUserAsync
        var usersNoBanco = await _context.Usuarios.ToListAsync();
        usersNoBanco.Should().HaveCount(1);
        usersNoBanco.First().Username.Should().Be(_testUsername);
    }

    [Fact]
    public async Task Edit_PostValido_AtualizaEnderecoERedirecionaParaIndex()
    {
        // Arrange
        var user = new Usuario { Username = _testUsername, Nome = "Test User", Senha = "123" };
        _context.Usuarios.Add(user);
        
        var endereco = new Endereco { Cep = "111", Logradouro = "Rua Antiga", Usuario = user };
        _context.Enderecos.Add(endereco);
        await _context.SaveChangesAsync();

        // Desanexa o objeto para simular comportamento real de post
        _context.Entry(endereco).State = EntityState.Detached;

        var enderecoModificado = new Endereco
        {
            Id = endereco.Id,
            Cep = "111",
            Logradouro = "Rua Nova",
            Numero = "1",
            Bairro = "Bairro",
            Cidade = "Cidade",
            Uf = "UF"
        };

        // Act
        var result = await _controller.Edit(endereco.Id, enderecoModificado);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");

        var enderecoNoBanco = await _context.Enderecos.FirstAsync(e => e.Id == endereco.Id);
        enderecoNoBanco.Logradouro.Should().Be("Rua Nova");
    }

    [Fact]
    public async Task ExportCsv_RetornaArquivoCsv()
    {
        // Arrange
        var user = new Usuario { Username = _testUsername, Nome = "Test User", Senha = "123" };
        _context.Usuarios.Add(user);
        
        var endereco = new Endereco { Cep = "111", Logradouro = "Rua 1", Usuario = user };
        _context.Enderecos.Add(endereco);
        await _context.SaveChangesAsync();

        var csvBytes = new byte[] { 1, 2, 3 };
        _csvExportServiceMock
            .Setup(s => s.ExportarEnderecosParaCsv(It.IsAny<IEnumerable<Endereco>>()))
            .Returns(csvBytes);

        // Act
        var result = await _controller.ExportCsv();

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("enderecos.csv");
        fileResult.FileContents.Should().BeEquivalentTo(csvBytes);
    }

    [Fact]
    public async Task BuscarCep_CepValido_RetornaJsonComDadosDoEndereco()
    {
        // Arrange
        var viaCepResponse = new ViaCepResponse
        {
            Cep = "01001-000",
            Logradouro = "Praça da Sé",
            Bairro = "Sé",
            Localidade = "São Paulo",
            Uf = "SP"
        };
        _viaCepServiceMock
            .Setup(s => s.BuscarCepAsync("01001000"))
            .ReturnsAsync(viaCepResponse);

        // Act
        var result = await _controller.BuscarCep("01001000");

        // Assert
        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.Value.Should().Be(viaCepResponse);
    }

    [Fact]
    public async Task BuscarCep_CepNaoEncontrado_RetornaNotFound()
    {
        // Arrange
        _viaCepServiceMock
            .Setup(s => s.BuscarCepAsync("99999999"))
            .ReturnsAsync((ViaCepResponse?)null);

        // Act
        var result = await _controller.BuscarCep("99999999");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- Exportação CSV ---------------------------------------------------
    //
    // Estes dois testes existem porque a exportação ficou um tempo filtrando por
    // dono mesmo para o administrador, que não tem endereços próprios e por isso
    // baixava um arquivo vazio. O que garante a correção é comparar quantos
    // endereços chegam ao serviço de CSV em cada caso.

    private async Task<List<Endereco>> SemearDoisUsuariosAsync()
    {
        var user = new Usuario { Username = _testUsername, Nome = "Test User", Senha = "123" };
        var outro = new Usuario { Username = "outro", Nome = "Outro", Senha = "123" };
        _context.Usuarios.AddRange(user, outro);

        _context.Enderecos.Add(new Endereco { Cep = "111", Logradouro = "Rua 1", Usuario = user });
        _context.Enderecos.Add(new Endereco { Cep = "222", Logradouro = "Rua 2", Usuario = user });
        _context.Enderecos.Add(new Endereco { Cep = "333", Logradouro = "Rua 3", Usuario = outro });
        await _context.SaveChangesAsync();

        return await _context.Enderecos.ToListAsync();
    }

    [Fact]
    public async Task ExportCsv_UsuarioComum_ExportaApenasOsProprios()
    {
        // Arrange
        await SemearDoisUsuariosAsync();
        IEnumerable<Endereco>? exportados = null;
        _csvExportServiceMock
            .Setup(s => s.ExportarEnderecosParaCsv(It.IsAny<IEnumerable<Endereco>>()))
            .Callback<IEnumerable<Endereco>>(e => exportados = e)
            .Returns(Array.Empty<byte>());

        // Act
        var result = await _controller.ExportCsv();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        exportados.Should().NotBeNull();
        exportados!.Should().HaveCount(2);
        exportados!.Should().OnlyContain(e => e.Usuario!.Username == _testUsername);
    }

    [Fact]
    public async Task ExportCsv_Admin_ExportaDeTodosOsUsuarios()
    {
        // Arrange
        await SemearDoisUsuariosAsync();
        _usuarioLocalServiceMock.Setup(s => s.EhAdmin(It.IsAny<ClaimsPrincipal>())).Returns(true);

        IEnumerable<Endereco>? exportados = null;
        _csvExportServiceMock
            .Setup(s => s.ExportarEnderecosParaCsv(It.IsAny<IEnumerable<Endereco>>()))
            .Callback<IEnumerable<Endereco>>(e => exportados = e)
            .Returns(Array.Empty<byte>());

        // Act
        var result = await _controller.ExportCsv();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        exportados.Should().NotBeNull();
        exportados!.Should().HaveCount(3, "o administrador exporta a base inteira, não só a dele");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
