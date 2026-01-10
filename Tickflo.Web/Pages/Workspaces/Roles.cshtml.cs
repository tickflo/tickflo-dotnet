using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class RolesModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRoleRepository _roleRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Role> Roles { get; private set; } = new();
    public Dictionary<int, int> RoleAssignmentCounts { get; private set; } = new();

    public RolesModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IRoleRepository roleRepo)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _roleRepo = roleRepo;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!TryGetUserId(out var uid)) return Forbid();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        Roles = await _roleRepo.ListForWorkspaceAsync(Workspace.Id);
        RoleAssignmentCounts = new Dictionary<int, int>();
        foreach (var r in Roles)
        {
            RoleAssignmentCounts[r.Id] = await _userWorkspaceRoleRepo.CountAssignmentsForRoleAsync(Workspace.Id, r.Id);
        }
        return Page();
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }
}
