using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GerenciamentoEndereco.API.Data;
using GerenciamentoEndereco.API.Services;
using GerenciamentoEndereco.API.Models.ViewModels;

namespace GerenciamentoEndereco.API.Controllers;

[Authorize(Policy = "UsuariosManage")]
public class UsuariosController : Controller
{
    private readonly AppDbContext _context;
    private readonly IKeycloakAdminService _keycloakAdminService;

    public UsuariosController(AppDbContext context, IKeycloakAdminService keycloakAdminService)
    {
        _context = context;
        _keycloakAdminService = keycloakAdminService;
    }

    public async Task<IActionResult> Index()
    {
        var localUsersList = await _context.Usuarios
            .Include(u => u.Enderecos)
            .ToListAsync();

        var localUsersDict = localUsersList.ToDictionary(u => u.Username);
        List<UsuarioListViewModel> viewModels = new();

        try
        {
            var keycloakUsers = await _keycloakAdminService.GetUsersAsync();
            if (keycloakUsers != null && keycloakUsers.Any())
            {
                viewModels = keycloakUsers.Select(u => new UsuarioListViewModel
                {
                    Id = u.Id,
                    Nome = string.IsNullOrWhiteSpace(u.Name) ? u.Username : u.Name,
                    Username = u.Username,
                    Email = u.Email,
                    Enabled = u.Enabled,
                    EnderecoCount = localUsersDict.ContainsKey(u.Username ?? "") ? (localUsersDict[u.Username!].Enderecos?.Count ?? 0) : 0
                }).OrderBy(u => u.Nome).ToList();
            }
        }
        catch
        {
            // Se o serviço admin do Keycloak não estiver acessível, cai para a base local abaixo
        }

        if (!viewModels.Any())
        {
            viewModels = localUsersList.Select(u => new UsuarioListViewModel
            {
                Id = u.Id.ToString(),
                Nome = string.IsNullOrWhiteSpace(u.Nome) ? u.Username : u.Nome,
                Username = u.Username,
                Email = u.Username.Contains("@") ? u.Username : $"{u.Username}@gerenciamento.com",
                Enabled = true,
                EnderecoCount = u.Enderecos?.Count ?? 0
            }).OrderBy(u => u.Nome).ToList();
        }

        return View(viewModels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateKeycloakUserRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        var success = await _keycloakAdminService.CreateUserAsync(request);
        if (success)
        {
            TempData["SuccessMessage"] = $"Usuário {request.Username} criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Erro ao criar o usuário. Verifique se o username já existe.");
        return View(request);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _keycloakAdminService.GetUserAsync(id);
        if (user == null) return NotFound();

        var viewModel = new EditUsuarioViewModel
        {
            Pk = 0,
            KeycloakId = user.Id,
            Username = user.Username,
            Name = user.Name ?? user.Username,
            Email = user.Email,
            IsActive = user.Enabled
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUsuarioViewModel viewModel)
    {
        if (id != viewModel.KeycloakId) return NotFound();
        if (!ModelState.IsValid) return View(viewModel);

        var parts = (viewModel.Name ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = parts.Length > 0 ? parts[0] : viewModel.Name;
        var lastName = parts.Length > 1 ? parts[1] : string.Empty;

        var success = await _keycloakAdminService.UpdateUserAsync(id, new UpdateKeycloakUserRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = viewModel.Email,
            Enabled = viewModel.IsActive,
            NovaSenha = viewModel.NovaSenha
        });

        if (success)
        {
            TempData["SuccessMessage"] = $"Usuário {viewModel.Username} atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Erro ao atualizar o usuário.");
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _keycloakAdminService.GetUserAsync(id);
        if (user == null) return NotFound();

        if (string.Equals(user.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Você não pode excluir o próprio usuário enquanto está logado com ele.";
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _keycloakAdminService.GetUserAsync(id);
        if (user != null && string.Equals(user.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Você não pode excluir o próprio usuário enquanto está logado com ele.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _keycloakAdminService.DeleteUserAsync(id);

        if (!success)
        {
            TempData["ErrorMessage"] = "Erro ao remover o usuário.";
            return RedirectToAction(nameof(Index));
        }

        // Excluir só no Keycloak deixava para trás o registro espelho e todos os
        // endereços da pessoa. Além de manter dado pessoal que deveria sumir
        // (LGPD), o vínculo antigo era pelo nome de usuário — então recriar uma
        // conta com o mesmo nome fazia a nova enxergar os endereços da anterior.
        var enderecosRemovidos = await RemoverDadosLocaisAsync(id, user?.Username);

        TempData["SuccessMessage"] = enderecosRemovidos > 0
            ? $"Usuário removido com sucesso, junto com {enderecosRemovidos} endereço(s)."
            : "Usuário removido com sucesso!";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Remove o registro local do usuário e os endereços dele.
    /// Retorna quantos endereços foram excluídos.
    /// </summary>
    /// <param name="keycloakId">Identificador do usuário no Keycloak.</param>
    /// <param name="username">Nome de usuário, usado para achar registros antigos
    /// criados antes da coluna KeycloakId existir.</param>
    private async Task<int> RemoverDadosLocaisAsync(string keycloakId, string? username)
    {
        var local = await _context.Usuarios
            .Include(u => u.Enderecos)
            .FirstOrDefaultAsync(u =>
                u.KeycloakId == keycloakId ||
                (u.KeycloakId == null && u.Username == username));

        if (local == null)
            return 0;

        var total = local.Enderecos?.Count ?? 0;

        if (local.Enderecos != null && local.Enderecos.Count > 0)
            _context.Enderecos.RemoveRange(local.Enderecos);

        _context.Usuarios.Remove(local);
        await _context.SaveChangesAsync();

        return total;
    }
}
