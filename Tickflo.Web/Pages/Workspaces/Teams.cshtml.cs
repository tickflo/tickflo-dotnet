using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Common;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceTeamsViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Team> Teams { get; private set; } = new();
    public Dictionary<int, int> MemberCounts { get; private set; } = new();
    public bool CanCreateTeams { get; private set; }
    public bool CanEditTeams { get; private set; }

    public TeamsModel(
        IWorkspaceRepository workspaces,
        ICurrentUserService currentUserService,
        IWorkspaceTeamsViewService viewService)
    {
        _workspaces = workspaces;
        _currentUserService = currentUserService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaces, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
        
        if (EnsurePermissionOrForbid(viewData.CanViewTeams) is IActionResult permCheck) return permCheck;

        Teams = viewData.Teams;
        MemberCounts = viewData.MemberCounts;
        CanCreateTeams = viewData.CanCreateTeams;
        CanEditTeams = viewData.CanEditTeams;

        return Page();
    }
}

