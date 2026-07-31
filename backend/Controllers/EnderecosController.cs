using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Models;
using GerenciamentoEndereco.API.Services;
using System.Security.Claims;

namespace GerenciamentoEndereco.API.Controllers;

[Authorize(Policy = "EnderecoRead")]
public class EnderecosController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICsvExportService _csvExportService;
    private readonly IViaCepService _viaCepService;

    public EnderecosController(AppDbContext context, ICsvExportService csvExportService, IViaCepService viaCepService)
    {
        _context = context;
        _csvExportService = csvExportService;
        _viaCepService = viaCepService;
    }

    /// <summary>
    /// Busca um endereço a partir do CEP via integração com a API do ViaCEP.
    /// Usado pelo formulário de cadastro/edição para autopreenchimento.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BuscarCep(string cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return BadRequest(new { erro = true, mensagem = "CEP não informado." });

        var resultado = await _viaCepService.BuscarCepAsync(cep);

        if (resultado == null)
            return NotFound(new { erro = true, mensagem = "CEP não encontrado." });

        return Json(resultado);
    }

    private async Task<Usuario> GetOrCreateLocalUserAsync()
    {
        var username = User.FindFirstValue("preferred_username") 
                    ?? User.FindFirstValue(ClaimTypes.Name) 
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
                    ?? User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
            throw new UnauthorizedAccessException("Usuário não identificado no sistema de autenticação.");

        var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
        if (localUser == null)
        {
            var name = User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.GivenName) ?? username;
            localUser = new Usuario { Username = username, Nome = name, Senha = "AUTHENTIK_MANAGED" };
            _context.Usuarios.Add(localUser);
            await _context.SaveChangesAsync();
        }

        return localUser;
    }

    public async Task<IActionResult> Index(string? username)
    {
        var isAdmin = User.HasClaim(c => c.Type == "roles" && c.Value == "admin") || 
                      User.HasClaim(c => c.Type == "client_role" && c.Value == "usuarios.manage");

        // Se for Admin e nenhum usuário específico foi selecionado, mostra TODOS os endereços
        if (isAdmin && string.IsNullOrEmpty(username))
        {
            var todosEnderecos = await _context.Enderecos
                .Include(e => e.Usuario)
                .ToListAsync();

            ViewData["TargetNome"] = "Todos os Usuários (Painel Admin)";
            ViewData["IsAllView"] = true;
            return View(todosEnderecos);
        }

        Usuario targetUser;
        if (!string.IsNullOrEmpty(username) && isAdmin)
        {
            targetUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
            if (targetUser == null)
            {
                // Se o usuário existe no Keycloak mas ainda não criou endereço local, inicializa na base local
                targetUser = new Usuario { Username = username, Nome = username, Senha = "KEYCLOAK_MANAGED" };
                _context.Usuarios.Add(targetUser);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            targetUser = await GetOrCreateLocalUserAsync();
        }

        var enderecos = await _context.Enderecos
            .Include(e => e.Usuario)
            .Where(e => e.UsuarioId == targetUser.Id)
            .ToListAsync();

        ViewData["TargetUsername"] = targetUser.Username;
        ViewData["TargetNome"] = targetUser.Nome;
        ViewData["IsAdminViewingOther"] = targetUser.Username != User.Identity?.Name;

        return View(enderecos);
    }

    [Authorize(Policy = "EnderecoWrite")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "EnderecoWrite")]
    public async Task<IActionResult> Create(Endereco endereco)
    {
        var user = await GetOrCreateLocalUserAsync();
        endereco.UsuarioId = user.Id;

        // Removemos a validação de Usuario, pois nós o preenchemos acima.
        ModelState.Remove("Usuario");

        if (ModelState.IsValid)
        {
            _context.Enderecos.Add(endereco);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(endereco);
    }

    [Authorize(Policy = "EnderecoWrite")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var user = await GetOrCreateLocalUserAsync();
        var endereco = await _context.Enderecos.FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == user.Id);

        if (endereco == null) return NotFound();

        return View(endereco);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "EnderecoWrite")]
    public async Task<IActionResult> Edit(int id, Endereco endereco)
    {
        if (id != endereco.Id) return NotFound();

        var user = await GetOrCreateLocalUserAsync();
        endereco.UsuarioId = user.Id;

        ModelState.Remove("Usuario");

        if (ModelState.IsValid)
        {
            try
            {
                // Verifica se o endereço pertence ao usuário antes de atualizar
                var existing = await _context.Enderecos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == user.Id);
                if (existing == null) return NotFound();

                _context.Update(endereco);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EnderecoExists(endereco.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(endereco);
    }

    [Authorize(Policy = "EnderecoDelete")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var user = await GetOrCreateLocalUserAsync();
        var endereco = await _context.Enderecos.FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == user.Id);

        if (endereco == null) return NotFound();

        return View(endereco);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "EnderecoDelete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await GetOrCreateLocalUserAsync();
        var endereco = await _context.Enderecos.FirstOrDefaultAsync(e => e.Id == id && e.UsuarioId == user.Id);
        
        if (endereco != null)
        {
            _context.Enderecos.Remove(endereco);
            await _context.SaveChangesAsync();
        }
        
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "EnderecoExport")]
    public async Task<IActionResult> ExportCsv()
    {
        var user = await GetOrCreateLocalUserAsync();
        var enderecos = await _context.Enderecos
            .Where(e => e.UsuarioId == user.Id)
            .ToListAsync();

        var bytes = _csvExportService.ExportarEnderecosParaCsv(enderecos);
        return File(bytes, "text/csv", "enderecos.csv");
    }

    private bool EnderecoExists(int id)
    {
        return _context.Enderecos.Any(e => e.Id == id);
    }
}
