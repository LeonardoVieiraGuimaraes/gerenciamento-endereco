using System.Security.Claims;
using FluentAssertions;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Services;
using Microsoft.EntityFrameworkCore;

namespace GerenciamentoEndereco.Tests.Services;

public class UsuarioLocalServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UsuarioLocalService _service;

    public UsuarioLocalServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new UsuarioLocalService(_context);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth", "preferred_username", ClaimTypes.Role));

    [Fact]
    public void ObterUsername_DeveUsarPreferredUsername()
    {
        var principal = Principal(new Claim("preferred_username", "leonardo"));

        _service.ObterUsername(principal).Should().Be("leonardo");
    }

    [Fact]
    public void ObterUsername_SemClaims_DeveRetornarNulo()
    {
        _service.ObterUsername(new ClaimsPrincipal(new ClaimsIdentity()))
            .Should().BeNull();
    }

    // A role do Keycloak chega em maiúsculas. Este teste existe por causa de um bug
    // real: a comparação era feita com "admin" minúsculo e nunca batia, fazendo o
    // administrador ser tratado como usuário comum.
    [Theory]
    [InlineData("ADMIN")]
    [InlineData("admin")]
    [InlineData("Admin")]
    public void EhAdmin_DeveIgnorarCaixaDaRole(string role)
    {
        var principal = Principal(new Claim("roles", role));

        _service.EhAdmin(principal).Should().BeTrue();
    }

    [Fact]
    public void EhAdmin_UsuarioComum_DeveSerFalso()
    {
        var principal = Principal(
            new Claim("preferred_username", "leonardo"),
            new Claim("roles", "USUARIO"));

        _service.EhAdmin(principal).Should().BeFalse();
    }

    [Fact]
    public async Task ObterOuCriarAsync_UsuarioNovo_DeveCriarRegistroLocal()
    {
        var principal = Principal(
            new Claim("preferred_username", "novo"),
            new Claim("name", "Usuário Novo"));

        var usuario = await _service.ObterOuCriarAsync(principal);

        usuario.Username.Should().Be("novo");
        usuario.Nome.Should().Be("Usuário Novo");
        (await _context.Usuarios.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ObterOuCriarAsync_ChamadoDuasVezes_NaoDeveDuplicar()
    {
        var principal = Principal(new Claim("preferred_username", "repetido"));

        var primeiro = await _service.ObterOuCriarAsync(principal);
        var segundo = await _service.ObterOuCriarAsync(principal);

        segundo.Id.Should().Be(primeiro.Id);
        (await _context.Usuarios.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ObterOuCriarAsync_UsuarioExistente_DeveReaproveitar()
    {
        _context.Usuarios.Add(new Usuario { Nome = "Já Existe", Username = "existente", Senha = "x" });
        await _context.SaveChangesAsync();

        var usuario = await _service.ObterOuCriarAsync(Principal(new Claim("preferred_username", "existente")));

        usuario.Nome.Should().Be("Já Existe");
        (await _context.Usuarios.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ObterOuCriarAsync_SemIdentificacao_DeveLancarNaoAutorizado()
    {
        var acao = () => _service.ObterOuCriarAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        await acao.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObterOuCriarAsync_SemClaimName_DeveUsarUsernameComoNome()
    {
        var usuario = await _service.ObterOuCriarAsync(Principal(new Claim("preferred_username", "semnome")));

        usuario.Nome.Should().Be("semnome");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
