using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsEditModel : WorkspacePageModel
{
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

    public TeamsEditModel(
        IWorkspaceRepository workspaces,
        ITeamManagementService teamService,
        IWorkspaceTeamsEditViewService teamsEditViewService)
    {
        _workspaces = workspaces;
        _teamService = teamService;
        _teamsEditViewService = teamsEditViewService;
    }
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }
    public bool CanCreateTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();
        Workspace = ws;
        var data = await _teamsEditViewService.BuildAsync(ws.Id, uid, id);
        CanViewTeams = data.CanViewTeams;
        CanEditTeams = data.CanEditTeams;
        CanCreateTeams = data.CanCreateTeams;
        if (EnsurePermissionOrForbid(CanViewTeams) is IActionResult permCheck) return permCheck;

        Id = id;
        WorkspaceUsers = data.WorkspaceUsers ?? new();
        if (id > 0)
        {
            var team = data.ExistingTeam;
            var teamCheck = EnsureEntityBelongsToWorkspace(team, ws.Id);
            if (teamCheck is not null) return teamCheck;
            Name = team.Name;
            Description = team.Description;
            SelectedMemberIds = (data.ExistingMemberIds ?? new()).ToList();
        }
        else
        {
            Name = string.Empty;
            Description = null;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();
        Workspace = ws;
        var data = await _teamsEditViewService.BuildAsync(ws.Id, uid, id);
        CanViewTeams = data.CanViewTeams;
        CanEditTeams = data.CanEditTeams;
        CanCreateTeams = data.CanCreateTeams;
        var allowed = id == 0 ? CanCreateTeams : CanEditTeams;
        if (!allowed) return Forbid();

        // Ensure users list is available even if we return Page() on validation errors
        WorkspaceUsers = data.WorkspaceUsers ?? new();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var descTrim = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ModelState.AddModelError(nameof(Name), "Team name is required");
            return Page();
        }
        try
        {
            if (id == 0)
            {
                var created = await _teamService.CreateTeamAsync(ws.Id, nameTrim, descTrim);
                // Persist selected members via service validation/sync
                var selectedIds = (SelectedMemberIds ?? new()).Distinct().ToList();
                await _teamService.SyncTeamMembersAsync(created.Id, ws.Id, selectedIds);
            }
            else
            {
                var updated = await _teamService.UpdateTeamAsync(id, nameTrim, descTrim);
                var selectedIds = (SelectedMemberIds ?? new()).Distinct().ToList();
                await _teamService.SyncTeamMembersAsync(updated.Id, ws.Id, selectedIds);
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        return RedirectToPage("/Workspaces/Teams", new { slug });
    }

}
