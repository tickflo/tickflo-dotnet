using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsAssignModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ITeamRepository _teams;
    private readonly ITeamMemberRepository _members;
    private readonly ITeamManagementService _teamService;
    private readonly IWorkspaceTeamsAssignViewService _teamsAssignViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Team? Team { get; private set; }
    public List<User> Members { get; private set; } = new();
    public List<User> WorkspaceUsers { get; private set; } = new();

    [BindProperty]
    public int SelectedUserId { get; set; }
    [BindProperty]
    public int TeamId { get; set; }

    public TeamsAssignModel(IWorkspaceRepository workspaces, ITeamRepository teams, ITeamMemberRepository members, ITeamManagementService teamService, IWorkspaceTeamsAssignViewService teamsAssignViewService)
    {
        _workspaces = workspaces;
        _teams = teams;
        _members = members;
        _teamService = teamService;
        _teamsAssignViewService = teamsAssignViewService;
    }
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int teamId)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();
        var data = await _teamsAssignViewService.BuildAsync(ws.Id, uid, teamId);
        CanViewTeams = data.CanViewTeams;
        CanEditTeams = data.CanEditTeams;
        if (EnsurePermissionOrForbid(CanEditTeams) is IActionResult editCheck) return editCheck;
        Workspace = ws;
        Team = data.Team;
        TeamId = teamId;
        Members = data.Members ?? new();
        WorkspaceUsers = data.WorkspaceUsers ?? new();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();
        var data = await _teamsAssignViewService.BuildAsync(ws.Id, uid, TeamId);
        Workspace = ws;
        Team = data.Team;
        CanEditTeams = data.CanEditTeams;
        if (!CanEditTeams) return Forbid();

        if (TeamId <= 0 || SelectedUserId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a user to add.");
            return await OnGetAsync(slug, TeamId);
        }
        // Use service to validate and sync single add
        if (EnsureEntityExistsOrNotFound(Team) is IActionResult teamCheck) return teamCheck;
        var currentMembers = await _members.ListMembersAsync(TeamId);
        var desired = currentMembers.Select(m => m.Id).ToList();
        desired.Add(SelectedUserId);
        try
        {
            await _teamService.SyncTeamMembersAsync(TeamId, Workspace!.Id, desired.Distinct().ToList());
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();
        var data = await _teamsAssignViewService.BuildAsync(ws.Id, uid, TeamId);
        Workspace = ws;
        Team = data.Team;
        CanEditTeams = data.CanEditTeams;
        if (EnsurePermissionOrForbid(CanEditTeams) is IActionResult editCheck) return editCheck;

        // Use service to validate and sync single remove
        var currentMembers = await _members.ListMembersAsync(TeamId);
        var desired = currentMembers.Select(m => m.Id).Where(id => id != userId).ToList();
        try
        {
            await _teamService.SyncTeamMembersAsync(TeamId, Workspace!.Id, desired);
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }

}
