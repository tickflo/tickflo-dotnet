namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Views;

[Authorize]
public class TeamsAssignModel(IWorkspaceRepository workspaces, ITeamRepository teams, ITeamMemberRepository members, ITeamManagementService teamService, IWorkspaceTeamsAssignViewService teamsAssignViewService) : WorkspacePageModel
{
    #region Constants
    private const string UserSelectionError = "Please select a user to add.";
    #endregion

    private readonly IWorkspaceRepository _workspaces = workspaces;
    private readonly ITeamRepository _teams = teams;
    private readonly ITeamMemberRepository _members = members;
    private readonly ITeamManagementService _teamService = teamService;
    private readonly IWorkspaceTeamsAssignViewService _teamsAssignViewService = teamsAssignViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Team? Team { get; private set; }
    public List<User> Members { get; private set; } = [];
    public List<User> WorkspaceUsers { get; private set; } = [];

    [BindProperty]
    public int SelectedUserId { get; set; }
    [BindProperty]
    public int TeamId { get; set; }
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int teamId)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeAndLoadWorkspaceDataAsync(slug, teamId) is IActionResult authResult)
        {
            return authResult;
        }

        if (this.EnsurePermissionOrForbid(this.CanEditTeams) is IActionResult editCheck)
        {
            return editCheck;
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeAndLoadWorkspaceDataAsync(slug, this.TeamId) is IActionResult authResult)
        {
            return authResult;
        }

        if (!this.CanEditTeams)
        {
            return this.Forbid();
        }

        if (!this.ValidateUserSelection())
        {
            return await this.OnGetAsync(slug, this.TeamId);
        }

        if (this.EnsureEntityExistsOrNotFound(this.Team) is IActionResult teamCheck)
        {
            return teamCheck;
        }

        try
        {
            await this.AddUserToTeamAsync(this.SelectedUserId);
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }

        return this.RedirectToTeamsAssignPage(slug);
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeAndLoadWorkspaceDataAsync(slug, this.TeamId) is IActionResult authResult)
        {
            return authResult;
        }

        if (this.EnsurePermissionOrForbid(this.CanEditTeams) is IActionResult editCheck)
        {
            return editCheck;
        }

        try
        {
            await this.RemoveUserFromTeamAsync(userId);
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }

        return this.RedirectToTeamsAssignPage(slug);
    }

    private async Task<IActionResult?> AuthorizeAndLoadWorkspaceDataAsync(string slug, int teamId)
    {
        var ws = await this._workspaces.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var data = await this._teamsAssignViewService.BuildAsync(ws!.Id, uid, teamId);
        this.CanViewTeams = data.CanViewTeams;
        this.CanEditTeams = data.CanEditTeams;
        this.Workspace = ws;
        this.Team = data.Team;
        this.TeamId = teamId;
        this.Members = data.Members ?? [];
        this.WorkspaceUsers = data.WorkspaceUsers ?? [];

        return null;
    }

    private bool ValidateUserSelection()
    {
        if (this.TeamId <= 0 || this.SelectedUserId <= 0)
        {
            this.ModelState.AddModelError(string.Empty, UserSelectionError);
            return false;
        }
        return true;
    }

    private async Task AddUserToTeamAsync(int userId)
    {
        var currentMembers = await this._members.ListMembersAsync(this.TeamId);
        var desired = currentMembers.Select(m => m.Id).ToList();
        desired.Add(userId);
        await this._teamService.SyncTeamMembersAsync(this.TeamId, this.Workspace!.Id, [.. desired.Distinct()]);
    }

    private async Task RemoveUserFromTeamAsync(int userId)
    {
        var currentMembers = await this._members.ListMembersAsync(this.TeamId);
        var desired = currentMembers.Select(m => m.Id).Where(id => id != userId).ToList();
        await this._teamService.SyncTeamMembersAsync(this.TeamId, this.Workspace!.Id, desired);
    }

    private IActionResult RedirectToTeamsAssignPage(string slug) => this.RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = this.TeamId });

}

