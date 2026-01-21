namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Views;

[Authorize]
public class TeamsEditModel(
    IWorkspaceRepository workspaces,
    ITeamManagementService teamService,
    IWorkspaceTeamsEditViewService teamsEditViewService) : WorkspacePageModel
{
    #region Constants
    private const int NewTeamId = 0;
    private const string TeamNameRequired = "Team name is required";
    #endregion

    private readonly IWorkspaceRepository _workspaces = workspaces;
    private readonly ITeamManagementService _teamService = teamService;
    private readonly IWorkspaceTeamsEditViewService _teamsEditViewService = teamsEditViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int Id { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string? Description { get; set; }
    [BindProperty]
    public List<int> SelectedMemberIds { get; set; } = [];

    public List<User> WorkspaceUsers { get; private set; } = [];
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }
    public bool CanCreateTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        var ws = await this._workspaces.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        this.Workspace = ws;
        var data = await this._teamsEditViewService.BuildAsync(ws!.Id, uid, id);

        this.CanViewTeams = data.CanViewTeams;
        this.CanEditTeams = data.CanEditTeams;
        this.CanCreateTeams = data.CanCreateTeams;
        this.WorkspaceUsers = data.WorkspaceUsers ?? [];

        if (this.EnsurePermissionOrForbid(this.CanViewTeams) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Id = id;
        if (id > 0)
        {
            this.LoadTeamDataFromExisting(data.ExistingTeam, ws.Id, data.ExistingMemberIds);
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        var ws = await this._workspaces.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        this.Workspace = ws;
        var data = await this._teamsEditViewService.BuildAsync(ws!.Id, uid, id);

        this.CanViewTeams = data.CanViewTeams;
        this.CanEditTeams = data.CanEditTeams;
        this.CanCreateTeams = data.CanCreateTeams;
        this.WorkspaceUsers = data.WorkspaceUsers ?? [];

        var allowed = id == NewTeamId ? this.CanCreateTeams : this.CanEditTeams;
        if (!allowed)
        {
            return this.Forbid();
        }

        var nameValidation = this.ValidateTeamName();
        if (nameValidation != null)
        {
            return nameValidation;
        }

        try
        {
            var team = id == NewTeamId
                ? await this.CreateTeamAsync(ws)
                : await this.UpdateTeamAsync(id, ws);

            return this.RedirectToPage("/Workspaces/Teams", new { slug });
        }
        catch (InvalidOperationException ex)
        {
            this.ModelState.AddModelError(string.Empty, ex.Message);
            return this.Page();
        }
    }

    private void LoadTeamDataFromExisting(Team? team, int workspaceId, IList<int>? existingMemberIds)
    {
        if (team == null)
        {
            return;
        }

        var teamCheck = this.EnsureEntityBelongsToWorkspace(team, workspaceId);
        if (teamCheck is not null)
        {
            throw new InvalidOperationException("Team does not belong to this workspace");
        }

        this.Name = team.Name;
        this.Description = team.Description;
        this.SelectedMemberIds = [.. existingMemberIds ?? []];
    }

    private IActionResult? ValidateTeamName()
    {
        var nameTrim = this.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            this.ModelState.AddModelError(nameof(this.Name), TeamNameRequired);
            return this.Page();
        }
        return null;
    }

    private async Task<Team> CreateTeamAsync(Workspace ws)
    {
        var nameTrim = this.Name?.Trim() ?? string.Empty;
        var descTrim = string.IsNullOrWhiteSpace(this.Description) ? null : this.Description.Trim();

        var created = await this._teamService.CreateTeamAsync(ws.Id, nameTrim, descTrim);
        var selectedIds = (this.SelectedMemberIds ?? []).Distinct().ToList();
        await this._teamService.SyncTeamMembersAsync(created.Id, ws.Id, selectedIds);

        return created;
    }

    private async Task<Team> UpdateTeamAsync(int id, Workspace ws)
    {
        var nameTrim = this.Name?.Trim() ?? string.Empty;
        var descTrim = string.IsNullOrWhiteSpace(this.Description) ? null : this.Description.Trim();

        var updated = await this._teamService.UpdateTeamAsync(id, nameTrim, descTrim);
        var selectedIds = (this.SelectedMemberIds ?? []).Distinct().ToList();
        await this._teamService.SyncTeamMembersAsync(updated.Id, ws.Id, selectedIds);

        return updated;
    }

}

