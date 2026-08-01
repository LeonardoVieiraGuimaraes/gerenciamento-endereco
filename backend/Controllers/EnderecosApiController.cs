using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Models.ViewModels;
using GerenciamentoEndereco.API.Services;

namespace GerenciamentoEndereco.API.Controllers;

/// <summary>
/// Gerencia os endereços cadastrados no sistema. Requer autenticação (cookie de sessão).
///
/// As permissões são as mesmas da interface web: um usuário comum só enxerga e
/// altera os próprios endereços; ADMIN enxerga todos.
/// </summary>
[Route("api/enderecos")]
[ApiController]
[Produces("application/json")]
[Authorize(Policy = "EnderecoRead")]
public class EnderecosApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IUsuarioLocalService _usuarios;

    public EnderecosApiController(AppDbContext context, IUsuarioLocalService usuarios)
    {
        _context = context;
        _usuarios = usuarios;
    }

    /// <summary>
    /// Busca um endereço garantindo que o usuário autenticado pode acessá-lo.
    /// Devolve o endereço, ou o resultado HTTP adequado (404 / 403).
    /// </summary>
    private async Task<(Endereco? endereco, ActionResult? erro)> ObterComPermissaoAsync(int id)
    {
        var endereco = await _context.Enderecos
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (endereco == null)
            return (null, NotFound());

        if (_usuarios.EhAdmin(User))
            return (endereco, null);

        var username = _usuarios.ObterUsername(User);
        if (endereco.Usuario?.Username != username)
            return (null, Forbid());

        return (endereco, null);
    }

    /// <summary>
    /// Lista os endereços do usuário autenticado (ou todos, se o usuário for ADMIN).
    /// </summary>
    /// <response code="200">Lista de endereços retornada com sucesso.</response>
    /// <response code="401">Usuário não autenticado.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Endereco>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Endereco>>> GetEnderecos()
    {
        if (_usuarios.EhAdmin(User))
        {
            return await _context.Enderecos.ToListAsync();
        }

        var username = _usuarios.ObterUsername(User);
        if (string.IsNullOrEmpty(username))
            return Forbid();

        return await _context.Enderecos
            .Where(e => e.Usuario!.Username == username)
            .ToListAsync();
    }

    /// <summary>
    /// Obtém um endereço específico pelo ID (somente se pertencer ao usuário autenticado, ou se for ADMIN).
    /// </summary>
    /// <param name="id">Identificador do endereço.</param>
    /// <response code="200">Endereço encontrado.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Endereço pertence a outro usuário.</response>
    /// <response code="404">Endereço não encontrado.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Endereco), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Endereco>> GetEndereco(int id)
    {
        var (endereco, erro) = await ObterComPermissaoAsync(id);
        return erro ?? (ActionResult<Endereco>)endereco!;
    }

    /// <summary>
    /// Cadastra um novo endereço para o usuário autenticado.
    /// </summary>
    /// <response code="201">Endereço criado. O cabeçalho Location aponta para o novo recurso.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Usuário não autenticado.</response>
    [HttpPost]
    [Authorize(Policy = "EnderecoWrite")]
    [ProducesResponseType(typeof(Endereco), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Endereco>> PostEndereco(EnderecoRequest request)
    {
        var usuario = await _usuarios.ObterOuCriarAsync(User);

        var endereco = new Endereco
        {
            Cep = request.Cep,
            Logradouro = request.Logradouro,
            Complemento = request.Complemento,
            Bairro = request.Bairro,
            Cidade = request.Cidade,
            Uf = request.Uf,
            Numero = request.Numero,
            UsuarioId = usuario.Id
        };

        _context.Enderecos.Add(endereco);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEndereco), new { id = endereco.Id }, endereco);
    }

    /// <summary>
    /// Atualiza um endereço existente (somente se pertencer ao usuário autenticado, ou se for ADMIN).
    /// </summary>
    /// <param name="id">Identificador do endereço.</param>
    /// <param name="request">Novos dados do endereço.</param>
    /// <response code="204">Endereço atualizado.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Endereço pertence a outro usuário.</response>
    /// <response code="404">Endereço não encontrado.</response>
    [HttpPut("{id}")]
    [Authorize(Policy = "EnderecoWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutEndereco(int id, EnderecoRequest request)
    {
        var (endereco, erro) = await ObterComPermissaoAsync(id);
        if (erro != null) return erro;

        // O dono (UsuarioId) não é alterado de propósito: um endereço não muda
        // de titular por uma requisição de atualização.
        endereco!.Cep = request.Cep;
        endereco.Logradouro = request.Logradouro;
        endereco.Complemento = request.Complemento;
        endereco.Bairro = request.Bairro;
        endereco.Cidade = request.Cidade;
        endereco.Uf = request.Uf;
        endereco.Numero = request.Numero;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Exclui um endereço (somente se pertencer ao usuário autenticado, ou se for ADMIN).
    /// </summary>
    /// <param name="id">Identificador do endereço.</param>
    /// <response code="204">Endereço excluído.</response>
    /// <response code="401">Usuário não autenticado.</response>
    /// <response code="403">Endereço pertence a outro usuário.</response>
    /// <response code="404">Endereço não encontrado.</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "EnderecoDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEndereco(int id)
    {
        var (endereco, erro) = await ObterComPermissaoAsync(id);
        if (erro != null) return erro;

        _context.Enderecos.Remove(endereco!);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
