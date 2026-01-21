namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Workspace;

[Authorize]
[Route("workspaces/{slug}/users/roles/{id:int}")]
public class RolesController(
    IWorkspaceRepository workspaces,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IRoleManagementService roleManagementService,
    IRoleRepository roles) : Controller
{
    private readonly IWorkspaceRepository _workspaces = workspaces;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly IRoleManagementService _roleManagementService = roleManagementService;
    private readonly IRoleRepository _roles = roles;

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        var ws = await this._workspaces.FindBySlugAsync(slug);
        if (ws == null)
        {
            return this.NotFound();
        }

        if (!this._currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Unauthorized();
        }

        var isAdmin = await this._workspaceAccessService.UserIsWorkspaceAdminAsync(uid, ws.Id);
        if (!isAdmin)
        {
            return this.Forbid();
        }

        var role = await this._roles.FindByIdAsync(id);
        if (role == null || role.WorkspaceId != ws.Id)
        {
            return this.NotFound();
        }

        // Use service to check if role can be deleted (guard against assignments)
        try
        {
            await this._roleManagementService.EnsureRoleCanBeDeletedAsync(ws.Id, id, role.Name);
        }
        catch (InvalidOperationException ex)
        {
            this.TempData["Error"] = ex.Message;
            return this.Redirect($"/workspaces/{slug}/roles");
        }

        await this._roles.DeleteAsync(id);
        return this.Redirect($"/workspaces/{slug}/roles");
    }
}

