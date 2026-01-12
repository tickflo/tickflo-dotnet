using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class FilesModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceFilesViewService _filesViewService;

    public FilesModel(
        IWorkspaceRepository workspaceRepository,
        IWorkspaceFilesViewService filesViewService)
    {
        _workspaceRepository = workspaceRepository;
        _filesViewService = filesViewService;
    }

    public Workspace? Workspace { get; set; }
    public int WorkspaceId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaceRepository, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        var data = await _filesViewService.BuildAsync(workspace.Id, uid);
        if (EnsurePermissionOrForbid(data.CanViewFiles) is IActionResult permCheck) return permCheck;

        Workspace = workspace;
        WorkspaceId = workspace.Id;
        return Page();
    }
}
