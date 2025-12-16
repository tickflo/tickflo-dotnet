using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Microsoft.AspNetCore.Http;

namespace Tickflo.Web.Controllers;

[Route("workspaces/{slug}/users/roles/{id:int}")]
public class RolesController : Controller
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly IRoleRepository _roles;
    private readonly IHttpContextAccessor _http;

    public RolesController(IWorkspaceRepository workspaces, IUserWorkspaceRoleRepository uwr, IRoleRepository roles, IHttpContextAccessor http)
    {
        _workspaces = workspaces;
        _uwr = uwr;
        _roles = roles;
        _http = http;
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
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
}
