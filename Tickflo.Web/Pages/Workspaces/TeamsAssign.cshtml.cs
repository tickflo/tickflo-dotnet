using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsAssignModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly ITeamRepository _teams;
    private readonly ITeamMemberRepository _members;
    private readonly IRolePermissionRepository _rolePerms;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Team? Team { get; private set; }
    public List<User> Members { get; private set; } = new();
    public List<User> WorkspaceUsers { get; private set; } = new();

    [BindProperty]
    public int SelectedUserId { get; set; }
    [BindProperty]
    public int TeamId { get; set; }

    public TeamsAssignModel(IWorkspaceRepository workspaces, IUserRepository users, IUserWorkspaceRepository userWorkspaces, IUserWorkspaceRoleRepository uwr, ITeamRepository teams, ITeamMemberRepository members, IRolePermissionRepository rolePerms)
    {
        _workspaces = workspaces;
        _users = users;
        _userWorkspaces = userWorkspaces;
        _uwr = uwr;
        _teams = teams;
        _members = members;
        _rolePerms = rolePerms;
    }
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int teamId)
    {
        var access = await EnsureAccessAsync(slug, teamId);
        if (access.failure != null) return access.failure;

        Workspace = access.workspace;
        Team = access.team;
        TeamId = teamId;
        CanViewTeams = access.canView;
        CanEditTeams = access.canEdit;
        Members = access.members;
        WorkspaceUsers = access.workspaceUsers;
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string slug)
    {
        var access = await EnsureAccessAsync(slug, TeamId);
        if (access.failure != null) return access.failure;

        Workspace = access.workspace;
        Team = access.team;
        CanEditTeams = access.canEdit;
        if (!CanEditTeams) return Forbid();

        if (TeamId <= 0 || SelectedUserId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a user to add.");
            return await OnGetAsync(slug, TeamId);
        }
        var team = Team;
        if (team == null) return NotFound();
        await _members.AddAsync(TeamId, SelectedUserId);
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId)
    {
        var access = await EnsureAccessAsync(slug, TeamId);
        if (access.failure != null) return access.failure;

        Workspace = access.workspace;
        Team = access.team;
        CanEditTeams = access.canEdit;
        if (!CanEditTeams) return Forbid();

        await _members.RemoveAsync(TeamId, userId);
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }

    private async Task<(Workspace? workspace, Team? team, List<User> workspaceUsers, List<User> members, bool canView, bool canEdit, IActionResult? failure)> EnsureAccessAsync(string slug, int teamId)
    {
        WorkspaceSlug = slug;

        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return (null, null, new(), new(), false, false, NotFound());

        if (!TryGetUserId(out var uid)) return (ws, null, new(), new(), false, false, Forbid());

        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(ws.Id, uid);
        var canView = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanView);
        var canEdit = isAdmin || (eff.TryGetValue("teams", out var tp2) && tp2.CanEdit);

        if (!canView) return (ws, null, new(), new(), false, false, Forbid());

        var team = await _teams.FindByIdAsync(teamId);
        if (team == null || team.WorkspaceId != ws.Id) return (ws, null, new(), new(), canView, canEdit, NotFound());

        var members = await _members.ListMembersAsync(teamId);
        var memberships = await _userWorkspaces.FindForWorkspaceAsync(ws.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var workspaceUsers = new List<User>();
        foreach (var id in userIds)
        {
            var u = await _users.FindByIdAsync(id);
            if (u != null) workspaceUsers.Add(u);
        }

        return (ws, team, workspaceUsers, members, canView, canEdit, null);
    }
}
