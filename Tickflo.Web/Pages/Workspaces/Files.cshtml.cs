using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class FilesModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IWorkspaceFilesViewService _filesViewService;

    public FilesModel(
        IWorkspaceRepository workspaceRepository,
        IUserWorkspaceRepository userWorkspaceRepo,
        IWorkspaceFilesViewService filesViewService)
    {
        _workspaceRepository = workspaceRepository;
        _userWorkspaceRepo = userWorkspaceRepo;
        _filesViewService = filesViewService;
    }

    public Workspace? Workspace { get; set; }
    public int WorkspaceId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepository, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        var data = await _filesViewService.BuildAsync(workspace.Id, uid);
        if (EnsurePermissionOrForbid(data.CanViewFiles) is IActionResult permCheck) return permCheck;

        Workspace = workspace;
        WorkspaceId = workspace.Id;
        return Page();
    }
}

