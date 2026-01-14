using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class RolesModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceRolesViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Role> Roles { get; private set; } = new();
    public Dictionary<int, int> RoleAssignmentCounts { get; private set; } = new();

    public RolesModel(
        IWorkspaceRepository workspaceRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ICurrentUserService currentUserService,
        IWorkspaceRolesViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _currentUserService = currentUserService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
        
        if (!viewData.IsAdmin) return Forbid();

        Roles = viewData.Roles;
        RoleAssignmentCounts = viewData.RoleAssignmentCounts;

        return Page();
    }
}

