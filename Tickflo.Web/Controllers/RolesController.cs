using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Services;

namespace Tickflo.Web.Controllers;

[Authorize]
[Route("workspaces/{slug}/users/roles/{id:int}")]
public class RolesController : Controller
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IRoleRepository _roles;

    public RolesController(
        IWorkspaceRepository workspaces,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService,
        IRoleManagementService roleManagementService,
        IRoleRepository roles)
    {
        _workspaces = workspaces;
        _currentUserService = currentUserService;
        _workspaceAccessService = workspaceAccessService;
        _roleManagementService = roleManagementService;
        _roles = roles;
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();

        if (!_currentUserService.TryGetUserId(User, out var uid)) return Unauthorized();

        var isAdmin = await _workspaceAccessService.UserIsWorkspaceAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        var role = await _roles.FindByIdAsync(id);
        if (role == null || role.WorkspaceId != ws.Id) return NotFound();

        // Use service to check if role can be deleted (guard against assignments)
        try
        {
            await _roleManagementService.EnsureRoleCanBeDeletedAsync(ws.Id, id, role.Name);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return Redirect($"/workspaces/{slug}/roles");
        }

        await _roles.DeleteAsync(id);
        return Redirect($"/workspaces/{slug}/roles");
    }
}
