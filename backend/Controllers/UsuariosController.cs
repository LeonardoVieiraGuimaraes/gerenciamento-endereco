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
    private readonly IAuthentikAdminService _authentikAdminService;

    public UsuariosController(AppDbContext context, IAuthentikAdminService authentikAdminService)
    {
        _context = context;
        _authentikAdminService = authentikAdminService;
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
            var authentikUsers = await _authentikAdminService.GetUsersAsync();
            if (authentikUsers != null && authentikUsers.Any())
            {
                viewModels = authentikUsers.Select(u => new UsuarioListViewModel
                {
                    Id = u.Pk.ToString(),
                    Nome = string.IsNullOrWhiteSpace(u.Name) ? u.Username : u.Name,
                    Username = u.Username,
                    Email = u.Email,
                    Enabled = u.IsActive,
                    EnderecoCount = localUsersDict.ContainsKey(u.Username ?? "") ? (localUsersDict[u.Username!].Enderecos?.Count ?? 0) : 0
                }).OrderBy(u => u.Nome).ToList();
            }
        }
        catch
        {
            // Se o serviço admin do Authentik não estiver acessível, cai para a base local abaixo
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
    public async Task<IActionResult> Create(CreateAuthentikUserRequest request)
    {
        if (!ModelState.IsValid) return View(request);

        var success = await _authentikAdminService.CreateUserAsync(request);
        if (success)
        {
            TempData["SuccessMessage"] = $"Usuário {request.Username} criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Erro ao criar o usuário. Verifique se o username já existe.");
        return View(request);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _authentikAdminService.GetUserAsync(id);
        if (user == null) return NotFound();

        var viewModel = new EditUsuarioViewModel
        {
            Pk = user.Pk,
            Username = user.Username,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditUsuarioViewModel viewModel)
    {
        if (id != viewModel.Pk) return NotFound();
        if (!ModelState.IsValid) return View(viewModel);

        var success = await _authentikAdminService.UpdateUserAsync(id, new UpdateAuthentikUserRequest
        {
            Name = viewModel.Name,
            Email = viewModel.Email,
            IsActive = viewModel.IsActive,
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
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _authentikAdminService.GetUserAsync(id);
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
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _authentikAdminService.GetUserAsync(id);
        if (user != null && string.Equals(user.Username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Você não pode excluir o próprio usuário enquanto está logado com ele.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _authentikAdminService.DeleteUserAsync(id);
        TempData["SuccessMessage"] = success ? "Usuário removido com sucesso!" : null;
        TempData["ErrorMessage"] = success ? null : "Erro ao remover o usuário.";
        return RedirectToAction(nameof(Index));
    }
}
