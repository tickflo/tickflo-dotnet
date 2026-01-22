namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class TeamsModel(
    IWorkspaceService workspaceService,
    IWorkspaceTeamsViewService workspaceTeamsViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
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

