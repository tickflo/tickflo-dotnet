using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsAssignModel : WorkspacePageModel
{
    #region Constants
    private const string UserSelectionError = "Please select a user to add.";
    #endregion

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
        
        if (await AuthorizeAndLoadWorkspaceDataAsync(slug, teamId) is IActionResult authResult)
            return authResult;
        
        if (EnsurePermissionOrForbid(CanEditTeams) is IActionResult editCheck)
            return editCheck;
        
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        if (await AuthorizeAndLoadWorkspaceDataAsync(slug, TeamId) is IActionResult authResult)
            return authResult;
        
        if (!CanEditTeams) return Forbid();

        if (!ValidateUserSelection())
            return await OnGetAsync(slug, TeamId);
        
        if (EnsureEntityExistsOrNotFound(Team) is IActionResult teamCheck)
            return teamCheck;
        
        try
        {
            await AddUserToTeamAsync(SelectedUserId);
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        
        return RedirectToTeamsAssignPage(slug);
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        
        if (await AuthorizeAndLoadWorkspaceDataAsync(slug, TeamId) is IActionResult authResult)
            return authResult;
        
        if (EnsurePermissionOrForbid(CanEditTeams) is IActionResult editCheck)
            return editCheck;

        try
        {
            await RemoveUserFromTeamAsync(userId);
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        
        return RedirectToTeamsAssignPage(slug);
    }

    private async Task<IActionResult?> AuthorizeAndLoadWorkspaceDataAsync(string slug, int teamId)
    {
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
            return result;
        
        if (!TryGetUserId(out var uid))
            return Forbid();
        
        var data = await _teamsAssignViewService.BuildAsync(ws!.Id, uid, teamId);
        CanViewTeams = data.CanViewTeams;
        CanEditTeams = data.CanEditTeams;
        Workspace = ws;
        Team = data.Team;
        TeamId = teamId;
        Members = data.Members ?? new();
        WorkspaceUsers = data.WorkspaceUsers ?? new();
        
        return null;
    }

    private bool ValidateUserSelection()
    {
        if (TeamId <= 0 || SelectedUserId <= 0)
        {
            ModelState.AddModelError(string.Empty, UserSelectionError);
            return false;
        }
        return true;
    }

    private async Task AddUserToTeamAsync(int userId)
    {
        var currentMembers = await _members.ListMembersAsync(TeamId);
        var desired = currentMembers.Select(m => m.Id).ToList();
        desired.Add(userId);
        await _teamService.SyncTeamMembersAsync(TeamId, Workspace!.Id, desired.Distinct().ToList());
    }

    private async Task RemoveUserFromTeamAsync(int userId)
    {
        var currentMembers = await _members.ListMembersAsync(TeamId);
        var desired = currentMembers.Select(m => m.Id).Where(id => id != userId).ToList();
        await _teamService.SyncTeamMembersAsync(TeamId, Workspace!.Id, desired);
    }

    private IActionResult RedirectToTeamsAssignPage(string slug)
    {
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }

}

