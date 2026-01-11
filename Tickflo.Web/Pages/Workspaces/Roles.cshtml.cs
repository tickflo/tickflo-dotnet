using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class RolesModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IRoleManagementService _roleManagementService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Role> Roles { get; private set; } = new();
    public Dictionary<int, int> RoleAssignmentCounts { get; private set; } = new();

    public RolesModel(
        IWorkspaceRepository workspaceRepo,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService,
        IRoleManagementService roleManagementService)
    {
        _workspaceRepo = workspaceRepo;
        _currentUserService = currentUserService;
        _workspaceAccessService = workspaceAccessService;
        _roleManagementService = roleManagementService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!_currentUserService.TryGetUserId(User, out var uid)) return Forbid();

        var isAdmin = await _workspaceAccessService.UserIsWorkspaceAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();

        // Use service to get roles
        Roles = await _roleManagementService.GetWorkspaceRolesAsync(Workspace.Id);
        
        // Count assignments for each role
        RoleAssignmentCounts = new Dictionary<int, int>();
        foreach (var role in Roles)
        {
            RoleAssignmentCounts[role.Id] = await _roleManagementService.CountRoleAssignmentsAsync(Workspace.Id, role.Id);
        }

        return Page();
    }
}
