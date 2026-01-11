using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class RolesAssignModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IRoleManagementService _roleManagementService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<User> Members { get; private set; } = new();
    public List<Role> Roles { get; private set; } = new();
    public Dictionary<int, List<Role>> UserRoles { get; private set; } = new();

    [BindProperty]
    public int SelectedUserId { get; set; }
    [BindProperty]
    public int SelectedRoleId { get; set; }

    public RolesAssignModel(
        IWorkspaceRepository workspaces,
        IUserRepository users,
        IUserWorkspaceRepository userWorkspaces,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService,
        IRoleManagementService roleManagementService)
    {
        _workspaces = workspaces;
        _users = users;
        _userWorkspaces = userWorkspaces;
        _currentUserService = currentUserService;
        _workspaceAccessService = workspaceAccessService;
        _roleManagementService = roleManagementService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var access = await EnsureAdminAccessAsync(slug);
        if (access.failure != null) return access.failure;

        var ws = access.workspace!;
        var uid = access.userId;

        var memberships = await _userWorkspaces.FindForWorkspaceAsync(ws.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        Members = new List<User>();
        foreach (var id in userIds)
        {
            var u = await _users.FindByIdAsync(id);
            if (u != null) Members.Add(u);
        }

        // Use service to get roles
        Roles = await _roleManagementService.GetWorkspaceRolesAsync(ws.Id);
        
        foreach (var id in userIds)
        {
            var roles = await _roleManagementService.GetUserRolesAsync(id, ws.Id);
            UserRoles[id] = roles;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug)
    {
        var access = await EnsureAdminAccessAsync(slug);
        if (access.failure != null) return access.failure;

        var ws = access.workspace!;
        var uid = access.userId;

        if (SelectedUserId <= 0 || SelectedRoleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select both a user and a role.");
            return await OnGetAsync(slug);
        }

        // Verify role belongs to workspace
        if (!await _roleManagementService.RoleBelongsToWorkspaceAsync(SelectedRoleId, ws.Id))
        {
            ModelState.AddModelError(string.Empty, "Invalid role selection.");
            return await OnGetAsync(slug);
        }

        // Use service to assign role
        try
        {
            await _roleManagementService.AssignRoleToUserAsync(SelectedUserId, ws.Id, SelectedRoleId, uid);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await OnGetAsync(slug);
        }

        var queryQ = Request.Query["Query"].ToString();
        return Redirect($"/workspaces/{slug}/users/roles/assign?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}");
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        var access = await EnsureAdminAccessAsync(slug);
        if (access.failure != null) return access.failure;

        var ws = access.workspace!;

        // Use service to remove role
        await _roleManagementService.RemoveRoleFromUserAsync(userId, ws.Id, roleId);
        
        var queryQ = Request.Query["Query"].ToString();
        return Redirect($"/workspaces/{slug}/users/roles/assign?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}");
    }

    private async Task<(Workspace? workspace, int userId, IActionResult? failure)> EnsureAdminAccessAsync(string slug)
    {
        WorkspaceSlug = slug;

        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return (null, 0, NotFound());

        Workspace = ws;

        if (!_currentUserService.TryGetUserId(User, out var uid)) return (ws, 0, Forbid());

        var isAdmin = await _workspaceAccessService.UserIsWorkspaceAdminAsync(uid, ws.Id);
        if (!isAdmin) return (ws, uid, Forbid());

        return (ws, uid, null);
    }
}
