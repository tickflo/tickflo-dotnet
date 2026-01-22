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
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IRoleManagementService roleManagementService,
    IRoleRepository roleRepository) : Controller
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IRoleManagementService roleManagementService = roleManagementService;
    private readonly IRoleRepository roleRepository = roleRepository;

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(string slug, int id)
    {
        var workspace = await this.workspaceRepository.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Unauthorized();
        }

        var isAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(uid, workspace.Id);
        if (!isAdmin)
        {
            return this.Forbid();
        }

        var role = await this.roleRepository.FindByIdAsync(id);
        if (role == null || role.WorkspaceId != ws.Id)
        {
            return this.NotFound();
        }

        // Use service to check if role can be deleted (guard against assignments)
        try
        {
            await this.roleManagementService.EnsureRoleCanBeDeletedAsync(ws.Id, id, role.Name);
        }
        catch (InvalidOperationException ex)
        {
            this.TempData["Error"] = ex.Message;
            return this.Redirect($"/workspaces/{slug}/roles");
        }

        await this.roleRepository.DeleteAsync(id);
        return this.Redirect($"/workspaces/{slug}/roles");
    }
}

