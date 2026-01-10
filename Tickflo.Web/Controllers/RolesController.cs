using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tickflo.Core.Data;

namespace Tickflo.Web.Controllers;

[Authorize]
[Route("workspaces/{slug}/users/roles/{id:int}")]
public class RolesController : Controller
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly IRoleRepository _roles;

    public RolesController(IWorkspaceRepository workspaces, IUserWorkspaceRoleRepository uwr, IRoleRepository roles)
    {
        _workspaces = workspaces;
        _uwr = uwr;
        _roles = roles;
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();

        if (!TryGetUserId(out var uid)) return Unauthorized();

        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        var role = await _roles.FindByIdAsync(id);
        if (role == null || role.WorkspaceId != ws.Id) return NotFound();

        // Guard: prevent deleting roles that have assignments
        var assignCount = await _uwr.CountAssignmentsForRoleAsync(ws.Id, id);
        if (assignCount > 0)
        {
            TempData["Error"] = $"Cannot delete role '{role.Name}' while {assignCount} user(s) are assigned. Unassign them first.";
            return Redirect($"/workspaces/{slug}/users/roles");
        }

        await _roles.DeleteAsync(id);
        return Redirect($"/workspaces/{slug}/roles");
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
