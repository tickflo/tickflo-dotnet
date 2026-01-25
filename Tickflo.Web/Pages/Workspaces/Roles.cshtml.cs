namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class RolesModel(
    IWorkspaceService workspaceService,
    IWorkspaceRolesViewService workspaceRolesViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceRolesViewService workspaceRolesViewService = workspaceRolesViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Role> Roles { get; private set; } = [];
    public Dictionary<int, int> RoleAssignmentCounts { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceRolesViewService.BuildAsync(this.Workspace.Id, uid);

        if (!viewData.IsAdmin)
        {
            return this.Forbid();
        }

        this.Roles = viewData.Roles;
        this.RoleAssignmentCounts = viewData.RoleAssignmentCounts;

        return this.Page();
    }
}

