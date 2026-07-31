using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using System.Security.Claims;

namespace GerenciamentoEndereco.API.Controllers;

/// <summary>
/// Consulta os endereços cadastrados no sistema. Requer autenticação (cookie de sessão).
/// </summary>
[Route("api/enderecos")]
[ApiController]
[Produces("application/json")]
[Authorize(Policy = "EnderecoRead")]
public class EnderecosApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public EnderecosApiController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin() =>
        User.HasClaim(c => c.Type == "roles" && (c.Value == "admin" || c.Value == "ADMIN")) ||
        User.HasClaim(c => c.Type == "client_role" && c.Value == "usuarios.manage");

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
        if (IsAdmin())
        {
            return await _context.Enderecos.ToListAsync();
        }

        var username = User.FindFirstValue("preferred_username")
                    ?? User.FindFirstValue(ClaimTypes.Name)
                    ?? User.Identity?.Name;

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
        var endereco = await _context.Enderecos.Include(e => e.Usuario).FirstOrDefaultAsync(e => e.Id == id);

        if (endereco == null)
        {
            return NotFound();
        }

        if (!IsAdmin())
        {
            var username = User.FindFirstValue("preferred_username")
                        ?? User.FindFirstValue(ClaimTypes.Name)
                        ?? User.Identity?.Name;

            if (endereco.Usuario?.Username != username)
                return Forbid();
        }

        return endereco;
    }
}
