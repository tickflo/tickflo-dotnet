namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;

[Authorize]
public class TeamsModel(
    IWorkspaceRepository workspaceRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IWorkspaceTeamsViewService workspaceTeamsViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceTeamsViewService workspaceTeamsViewService = workspaceTeamsViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<Team> Teams { get; private set; } = [];
    public Dictionary<int, int> MemberCounts { get; private set; } = [];
    public bool CanCreateTeams { get; private set; }
    public bool CanEditTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;

        var viewData = await this.workspaceTeamsViewService.BuildAsync(this.Workspace!.Id, uid);

        if (this.EnsurePermissionOrForbid(viewData.CanViewTeams) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Teams = viewData.Teams;
        this.MemberCounts = viewData.MemberCounts;
        this.CanCreateTeams = viewData.CanCreateTeams;
        this.CanEditTeams = viewData.CanEditTeams;

        return this.Page();
    }
}

