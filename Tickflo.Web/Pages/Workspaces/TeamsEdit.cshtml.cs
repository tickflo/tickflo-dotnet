using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Views;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsEditModel : WorkspacePageModel
{
    #region Constants
    private const int NewTeamId = 0;
    private const string TeamNameRequired = "Team name is required";
    #endregion

    private readonly IWorkspaceRepository _workspaces;
    private readonly ITeamManagementService _teamService;
    private readonly IWorkspaceTeamsEditViewService _teamsEditViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int Id { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string? Description { get; set; }
    [BindProperty]
    public List<int> SelectedMemberIds { get; set; } = new();

    public List<User> WorkspaceUsers { get; private set; } = new();
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }
    public bool CanCreateTeams { get; private set; }

    public TeamsEditModel(
        IWorkspaceRepository workspaces,
        ITeamManagementService teamService,
        IWorkspaceTeamsEditViewService teamsEditViewService)
    {
        _workspaces = workspaces;
        _teamService = teamService;
        _teamsEditViewService = teamsEditViewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();

        Workspace = ws;
        var data = await _teamsEditViewService.BuildAsync(ws!.Id, uid, id);
        
        CanViewTeams = data.CanViewTeams;
        CanEditTeams = data.CanEditTeams;
        CanCreateTeams = data.CanCreateTeams;
        WorkspaceUsers = data.WorkspaceUsers ?? new();

        if (EnsurePermissionOrForbid(CanViewTeams) is IActionResult permCheck) return permCheck;

        Id = id;
        if (id > 0)
            LoadTeamDataFromExisting(data.ExistingTeam, ws.Id, data.ExistingMemberIds);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();

        Workspace = ws;
        var data = await _teamsEditViewService.BuildAsync(ws!.Id, uid, id);
        
        CanViewTeams = data.CanViewTeams;
        CanEditTeams = data.CanEditTeams;
        CanCreateTeams = data.CanCreateTeams;
        WorkspaceUsers = data.WorkspaceUsers ?? new();

        var allowed = id == NewTeamId ? CanCreateTeams : CanEditTeams;
        if (!allowed) return Forbid();

        var nameValidation = ValidateTeamName();
        if (nameValidation != null) return nameValidation;

        try
        {
            var team = id == NewTeamId
                ? await CreateTeamAsync(ws)
                : await UpdateTeamAsync(id, ws);

            return RedirectToPage("/Workspaces/Teams", new { slug });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private void LoadTeamDataFromExisting(Team? team, int workspaceId, IList<int>? existingMemberIds)
    {
        if (team == null) return;
        
        var teamCheck = EnsureEntityBelongsToWorkspace(team, workspaceId);
        if (teamCheck is not null) throw new InvalidOperationException("Team does not belong to this workspace");

        Name = team.Name;
        Description = team.Description;
        SelectedMemberIds = (existingMemberIds ?? new List<int>()).ToList();
    }

    private IActionResult? ValidateTeamName()
    {
        var nameTrim = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ModelState.AddModelError(nameof(Name), TeamNameRequired);
            return Page();
        }
        return null;
    }

    private async Task<Team> CreateTeamAsync(Workspace ws)
    {
        var nameTrim = Name?.Trim() ?? string.Empty;
        var descTrim = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        
        var created = await _teamService.CreateTeamAsync(ws.Id, nameTrim, descTrim);
        var selectedIds = (SelectedMemberIds ?? new()).Distinct().ToList();
        await _teamService.SyncTeamMembersAsync(created.Id, ws.Id, selectedIds);
        
        return created;
    }

    private async Task<Team> UpdateTeamAsync(int id, Workspace ws)
    {
        var nameTrim = Name?.Trim() ?? string.Empty;
        var descTrim = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        
        var updated = await _teamService.UpdateTeamAsync(id, nameTrim, descTrim);
        var selectedIds = (SelectedMemberIds ?? new()).Distinct().ToList();
        await _teamService.SyncTeamMembersAsync(updated.Id, ws.Id, selectedIds);
        
        return updated;
    }

}

