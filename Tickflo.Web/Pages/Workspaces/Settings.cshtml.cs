using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Tickflo.Web.Pages.Workspaces;

public class SettingsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITicketStatusRepository _statusRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public SettingsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IHttpContextAccessor httpContextAccessor, ITicketStatusRepository statusRepo)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _httpContextAccessor = httpContextAccessor;
        _statusRepo = statusRepo;
    }

    public IReadOnlyList<Tickflo.Core.Entities.TicketStatus> Statuses { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketStatus>();

    [BindProperty]
    public string? NewStatusName { get; set; }
    [BindProperty]
    public string? NewStatusColor { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        // Load or bootstrap default statuses
        var list = await _statusRepo.ListAsync(Workspace.Id);
        if (list.Count == 0)
        {
            var defaults = new[]
            {
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = Workspace.Id, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = Workspace.Id, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = Workspace.Id, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
            };
            foreach (var s in defaults)
            {
                await _statusRepo.CreateAsync(s);
            }
            list = await _statusRepo.ListAsync(Workspace.Id);
        }
        Statuses = list;
        return Page();
    }

    public async Task<IActionResult> OnPostAddStatusAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        var name = (NewStatusName ?? string.Empty).Trim();
        var color = (NewStatusColor ?? "neutral").Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var exists = await _statusRepo.FindByNameAsync(Workspace.Id, name);
            if (exists == null)
            {
                var maxOrder = (await _statusRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(s => s?.SortOrder ?? 0);
                await _statusRepo.CreateAsync(new Tickflo.Core.Entities.TicketStatus
                {
                    WorkspaceId = Workspace.Id,
                    Name = name,
                    Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color,
                    SortOrder = maxOrder + 1
                });
            }
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(string slug, int id, string name, string color, int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        var s = await _statusRepo.FindByIdAsync(Workspace.Id, id);
        if (s == null) return NotFound();
        s.Name = (name ?? s.Name).Trim();
        s.Color = string.IsNullOrWhiteSpace(color)
            ? (string.IsNullOrWhiteSpace(s.Color) ? "neutral" : s.Color)
            : color.Trim();
        s.SortOrder = sortOrder;
        await _statusRepo.UpdateAsync(s);
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteStatusAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        await _statusRepo.DeleteAsync(Workspace.Id, id);
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }
}
