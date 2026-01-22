namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Views;

[Authorize]
public class FilesModel(
    IWorkspaceRepository workspaceRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IWorkspaceFilesViewService workspaceFilesViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceFilesViewService workspaceFilesViewService = workspaceFilesViewService;

    public Workspace? Workspace { get; set; }
    public int WorkspaceId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        var data = await this.workspaceFilesViewService.BuildAsync(workspace!.Id, uid);
        if (this.EnsurePermissionOrForbid(data.CanViewFiles) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Workspace = workspace;
        this.WorkspaceId = workspace.Id;
        return this.Page();
    }
}

