using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TeamsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly ITeamRepository _teams;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _users;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IRolePermissionRepository _rolePerms;

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
        IUserWorkspaceRoleRepository uwr,
        ITeamRepository teams,
        IUserWorkspaceRepository userWorkspaces,
        IUserRepository users,
        ITeamMemberRepository teamMembers,
        IRolePermissionRepository rolePerms)
    {
        _workspaces = workspaces;
        _uwr = uwr;
        _teams = teams;
        _userWorkspaces = userWorkspaces;
        _users = users;
        _teamMembers = teamMembers;
        _rolePerms = rolePerms;
    }
    public bool CanViewTeams { get; private set; }
    public bool CanEditTeams { get; private set; }
    public bool CanCreateTeams { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        var access = await EnsureAccessAsync(slug);
        if (access.failure != null) return access.failure;

        var ws = access.workspace;
        if (ws == null) return NotFound();

        Workspace = ws;
        var uid = access.userId;
        CanViewTeams = access.canView;
        CanEditTeams = access.canEdit;
        CanCreateTeams = access.canCreate;

        Id = id;
        await LoadWorkspaceUsersAsync();
        if (id > 0)
        {
            var team = await _teams.FindByIdAsync(id);
            if (team == null || team.WorkspaceId != ws.Id) return NotFound();
            Name = team.Name;
            Description = team.Description;
            // Preselect existing members for edit
            var members = await _teamMembers.ListMembersAsync(team.Id);
            SelectedMemberIds = members.Select(m => m.Id).ToList();
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
        var access = await EnsureAccessAsync(slug);
        if (access.failure != null) return access.failure;

        var ws = access.workspace;
        if (ws == null) return NotFound();

        Workspace = ws;
        var uid = access.userId;
        CanViewTeams = access.canView;
        CanEditTeams = access.canEdit;
        CanCreateTeams = access.canCreate;

        var allowed = id == 0 ? CanCreateTeams : CanEditTeams;
        if (!allowed) return Forbid();

        // Ensure users list is available even if we return Page() on validation errors
        await LoadWorkspaceUsersAsync();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var descTrim = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ModelState.AddModelError(nameof(Name), "Team name is required");
            return Page();
        }
        var existingByName = await _teams.FindByNameAsync(ws.Id, nameTrim);
        if (id == 0)
        {
            if (existingByName != null)
            {
                ModelState.AddModelError(nameof(Name), "A team with that name already exists");
                return Page();
            }
            var created = await _teams.AddAsync(ws.Id, nameTrim, descTrim, uid);
            // Persist selected members only on create
            if (SelectedMemberIds?.Count > 0)
            {
                // Validate users belong to this workspace (accepted)
                var acceptedUws = await _userWorkspaces.FindForWorkspaceAsync(ws.Id);
                var validUserIds = acceptedUws.Where(uw => uw.Accepted).Select(uw => uw.UserId).ToHashSet();
                foreach (var memberId in SelectedMemberIds.Distinct())
                {
                    if (validUserIds.Contains(memberId))
                    {
                        await _teamMembers.AddAsync(created.Id, memberId);
                    }
                }
            }
        }
        else
        {
            var team = await _teams.FindByIdAsync(id);
            if (team == null || team.WorkspaceId != ws.Id) return NotFound();
            if (existingByName != null && existingByName.Id != team.Id)
            {
                ModelState.AddModelError(nameof(Name), "A team with that name already exists");
                return Page();
            }
            team.Name = nameTrim;
            team.Description = descTrim;
            team.UpdatedAt = DateTime.UtcNow;
            team.UpdatedBy = uid;
            await _teams.UpdateAsync(team);
            // Sync membership to match SelectedMemberIds on edit
            var currentMembers = await _teamMembers.ListMembersAsync(team.Id);
            var currentIds = currentMembers.Select(m => m.Id).ToHashSet();
            var desiredIds = (SelectedMemberIds ?? new()).ToHashSet();

            // Validate desired IDs belong to this workspace (accepted)
            var acceptedUws = await _userWorkspaces.FindForWorkspaceAsync(ws.Id);
            var validUserIds = acceptedUws.Where(uw => uw.Accepted).Select(uw => uw.UserId).ToHashSet();
            desiredIds.IntersectWith(validUserIds);

            var toAdd = desiredIds.Except(currentIds).ToList();
            var toRemove = currentIds.Except(desiredIds).ToList();
            foreach (var addId in toAdd)
            {
                await _teamMembers.AddAsync(team.Id, addId);
            }
            foreach (var removeId in toRemove)
            {
                await _teamMembers.RemoveAsync(team.Id, removeId);
            }
        }
        return RedirectToPage("/Workspaces/Teams", new { slug });
    }

    private async Task LoadWorkspaceUsersAsync()
    {
        if (Workspace == null) { WorkspaceUsers = new(); return; }
        var uws = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
        var accepted = uws.Where(uw => uw.Accepted).Select(uw => uw.UserId).ToList();
        var allUsers = await _users.ListAsync();
        WorkspaceUsers = allUsers.Where(u => accepted.Contains(u.Id)).OrderBy(u => u.Name).ToList();
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

    private async Task<(Workspace? workspace, int userId, bool canView, bool canEdit, bool canCreate, IActionResult? failure)> EnsureAccessAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return (null, 0, false, false, false, NotFound());

        if (!TryGetUserId(out var userId)) return (ws, 0, false, false, false, Forbid());

        var isAdmin = await _uwr.IsAdminAsync(userId, ws.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(ws.Id, userId);

        if (isAdmin)
        {
            return (ws, userId, true, true, true, null);
        }

        if (!eff.TryGetValue("teams", out var tp))
        {
            return (ws, userId, false, false, false, Forbid());
        }

        return (ws, userId, tp.CanView, tp.CanEdit, tp.CanCreate, tp.CanView ? null : Forbid());
    }
}
