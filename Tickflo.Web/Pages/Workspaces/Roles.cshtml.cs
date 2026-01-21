namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;

[Authorize]
public class RolesModel(
    IWorkspaceRepository workspaceRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    ICurrentUserService currentUserService,
    IWorkspaceRolesViewService viewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IWorkspaceRolesViewService _viewService = viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Role> Roles { get; private set; } = [];
    public Dictionary<int, int> RoleAssignmentCounts { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this._workspaceRepo, this._userWorkspaceRepo, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, uid);

        if (!viewData.IsAdmin)
        {
            return this.Forbid();
        }

        this.Roles = viewData.Roles;
        this.RoleAssignmentCounts = viewData.RoleAssignmentCounts;

        return this.Page();
    }
}

