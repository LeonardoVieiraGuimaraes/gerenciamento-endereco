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

    /// <summary>Principal com nome de usuário e identificador do Keycloak (sub).</summary>
    private static ClaimsPrincipal PrincipalCom(string username, string sub) =>
        Principal(
            new Claim("preferred_username", username),
            new Claim(ClaimTypes.NameIdentifier, sub));

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

    // ---------- Vínculo pelo identificador do Keycloak (issue #33) ----------

    /// <summary>
    /// O teste central da correção: uma conta excluída e recriada com o MESMO
    /// nome de usuário recebe um "sub" novo, e não pode herdar o registro (nem os
    /// endereços) da conta anterior.
    /// </summary>
    [Fact]
    public async Task ObterOuCriarAsync_MesmoUsernameComSubDiferente_NaoDeveReaproveitarRegistro()
    {
        var antiga = await _service.ObterOuCriarAsync(PrincipalCom("maria", "sub-antigo"));

        var nova = await _service.ObterOuCriarAsync(PrincipalCom("maria", "sub-novo"));

        nova.Id.Should().NotBe(antiga.Id, "cada identidade do Keycloak precisa do próprio registro local");
        nova.KeycloakId.Should().Be("sub-novo");
        (await _context.Usuarios.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task ObterOuCriarAsync_MesmoSub_DeveReaproveitarRegistro()
    {
        var primeiro = await _service.ObterOuCriarAsync(PrincipalCom("joao", "sub-123"));
        var segundo = await _service.ObterOuCriarAsync(PrincipalCom("joao", "sub-123"));

        segundo.Id.Should().Be(primeiro.Id);
        (await _context.Usuarios.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Trocar o nome de usuário no Keycloak não cria pessoa nova: o "sub" continua
    /// o mesmo, então o registro local é reaproveitado e o nome é atualizado.
    /// </summary>
    [Fact]
    public async Task ObterOuCriarAsync_UsernameAlterado_DeveManterRegistroEAtualizarNome()
    {
        var original = await _service.ObterOuCriarAsync(PrincipalCom("joao.silva", "sub-123"));

        var depois = await _service.ObterOuCriarAsync(PrincipalCom("joao.souza", "sub-123"));

        depois.Id.Should().Be(original.Id);
        depois.Username.Should().Be("joao.souza");
        (await _context.Usuarios.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Registro criado antes da coluna existir (KeycloakId nulo) é adotado no
    /// primeiro acesso e passa a ficar vinculado ao "sub".
    /// </summary>
    [Fact]
    public async Task ObterOuCriarAsync_RegistroLegado_DeveAdotarEPreencherKeycloakId()
    {
        _context.Usuarios.Add(new Usuario { Nome = "Legado", Username = "legado", Senha = "x" });
        await _context.SaveChangesAsync();

        var usuario = await _service.ObterOuCriarAsync(PrincipalCom("legado", "sub-legado"));

        usuario.Nome.Should().Be("Legado");
        usuario.KeycloakId.Should().Be("sub-legado");
        (await _context.Usuarios.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Uma vez vinculado a um "sub", o registro não pode mais ser adotado por
    /// outra identidade — é o que fecha a brecha de reuso de nome.
    /// </summary>
    [Fact]
    public async Task ObterOuCriarAsync_RegistroJaVinculado_NaoDeveSerAdotadoPorOutroSub()
    {
        _context.Usuarios.Add(new Usuario
        {
            Nome = "Dono",
            Username = "compartilhado",
            Senha = "x",
            KeycloakId = "sub-dono"
        });
        await _context.SaveChangesAsync();

        var intruso = await _service.ObterOuCriarAsync(PrincipalCom("compartilhado", "sub-intruso"));

        intruso.KeycloakId.Should().Be("sub-intruso");
        intruso.Nome.Should().NotBe("Dono");
        (await _context.Usuarios.CountAsync()).Should().Be(2);
    }

    /// <summary>
    /// O "sub" é um GUID e nunca deve acabar na coluna Username — o que aconteceria
    /// se a busca do nome caísse em ClaimTypes.NameIdentifier.
    /// </summary>
    [Fact]
    public void ObterUsername_SemNomeMasComSub_NaoDeveRetornarOSub()
    {
        var principal = Principal(new Claim(ClaimTypes.NameIdentifier, "3f2504e0-4f89-11d3-9a0c-0305e82c3301"));

        _service.ObterUsername(principal).Should().BeNull();
    }

    [Fact]
    public void ObterKeycloakId_DeveLerOSub()
    {
        _service.ObterKeycloakId(PrincipalCom("x", "sub-abc")).Should().Be("sub-abc");
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
