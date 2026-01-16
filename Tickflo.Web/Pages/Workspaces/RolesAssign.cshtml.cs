using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Roles;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class RolesAssignModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IWorkspaceRolesAssignViewService _rolesAssignViewService;

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
        IRoleManagementService roleManagementService,
        IWorkspaceRolesAssignViewService rolesAssignViewService)
    {
        _workspaces = workspaces;
        _roleManagementService = roleManagementService;
        _rolesAssignViewService = rolesAssignViewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _rolesAssignViewService.BuildAsync(ws!.Id, uid);
        if (!data.IsAdmin) return Forbid();
        Members = data.Members ?? new();
        Roles = data.Roles ?? new();
        UserRoles = data.UserRoles ?? new();
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _rolesAssignViewService.BuildAsync(ws!.Id, uid);
        if (!data.IsAdmin) return Forbid();

        if (SelectedUserId <= 0 || SelectedRoleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select both a user and a role.");
            return await OnGetAsync(slug);
        }

        // Verify role belongs to workspace
        if (!await _roleManagementService.RoleBelongsToWorkspaceAsync(SelectedRoleId, ws!.Id))
        {
            ModelState.AddModelError(string.Empty, "Invalid role selection.");
            return await OnGetAsync(slug);
        }

        // Use service to assign role
        try
        {
            await _roleManagementService.AssignRoleToUserAsync(SelectedUserId, ws!.Id, SelectedRoleId, uid);
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
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _rolesAssignViewService.BuildAsync(ws!.Id, uid);
        if (!data.IsAdmin) return Forbid();

        // Use service to remove role
        await _roleManagementService.RemoveRoleFromUserAsync(userId, ws!.Id, roleId);
        
        var queryQ = Request.Query["Query"].ToString();
        return Redirect($"/workspaces/{slug}/users/roles/assign?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}");
    }
}

