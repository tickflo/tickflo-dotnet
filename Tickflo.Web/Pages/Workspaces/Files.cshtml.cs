namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class FilesModel(
    IWorkspaceService workspaceService,
    IWorkspaceFilesViewService workspaceFilesViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceFilesViewService workspaceFilesViewService = workspaceFilesViewService;

    public Workspace? Workspace { get; set; }
    public int WorkspaceId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var data = await this.workspaceFilesViewService.BuildAsync(workspace.Id, uid);
        if (this.EnsurePermissionOrForbid(data.CanViewFiles) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Workspace = workspace;
        this.WorkspaceId = workspace.Id;
        return this.Page();
    }
}

