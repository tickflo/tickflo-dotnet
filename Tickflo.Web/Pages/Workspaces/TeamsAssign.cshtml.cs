using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

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
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, Workspace.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, uid);
        CanViewTeams = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanView);
        CanEditTeams = isAdmin || (eff.TryGetValue("teams", out var tp2) && tp2.CanEdit);
        if (!CanViewTeams) return Forbid();

        TeamId = teamId;
        Team = await _teams.FindByIdAsync(teamId);
        if (Team == null || Team.WorkspaceId != Workspace.Id) return NotFound();
        Members = await _members.ListMembersAsync(teamId);
        var memberships = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        WorkspaceUsers = new List<User>();
        foreach (var id in userIds)
        {
            var u = await _users.FindByIdAsync(id);
            if (u != null) WorkspaceUsers.Add(u);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, Workspace.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, uid);
        var allowed = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanEdit);
        if (!allowed) return Forbid();

        if (TeamId <= 0 || SelectedUserId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select a user to add.");
            return await OnGetAsync(slug, TeamId);
        }
        var team = await _teams.FindByIdAsync(TeamId);
        if (team == null || team.WorkspaceId != Workspace.Id) return NotFound();
        await _members.AddAsync(TeamId, SelectedUserId);
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaces.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, Workspace.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, uid);
        var allowed = isAdmin || (eff.TryGetValue("teams", out var tp) && tp.CanEdit);
        if (!allowed) return Forbid();

        await _members.RemoveAsync(TeamId, userId);
        return RedirectToPage("/Workspaces/TeamsAssign", new { slug, teamId = TeamId });
    }
}
