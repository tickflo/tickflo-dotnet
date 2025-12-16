using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Tickflo.Web.Pages.Workspaces;

public class RolesAssignModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly IRoleRepository _roles;
    private readonly IHttpContextAccessor _http;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<User> Members { get; private set; } = new();
    public List<Role> Roles { get; private set; } = new();
    public Dictionary<int, List<Role>> UserRoles { get; private set; } = new();

    [BindProperty]
    public int SelectedUserId { get; set; }
    [BindProperty]
    public int SelectedRoleId { get; set; }

    public RolesAssignModel(IWorkspaceRepository workspaces, IUserRepository users, IUserWorkspaceRepository userWorkspaces, IUserWorkspaceRoleRepository uwr, IRoleRepository roles, IHttpContextAccessor http)
    {
        _workspaces = workspaces;
        _users = users;
        _userWorkspaces = userWorkspaces;
        _uwr = uwr;
        _roles = roles;
        _http = http;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        Workspace = ws;
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        var memberships = await _userWorkspaces.FindForWorkspaceAsync(ws.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        Members = new List<User>();
        foreach (var id in userIds)
        {
            var u = await _users.FindByIdAsync(id);
            if (u != null) Members.Add(u);
        }
        Roles = await _roles.ListForWorkspaceAsync(ws.Id);
        foreach (var id in userIds)
        {
            var roles = await _uwr.GetRolesAsync(id, ws.Id);
            UserRoles[id] = roles;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        if (SelectedUserId <= 0 || SelectedRoleId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Please select both a user and a role.");
            return await OnGetAsync(slug);
        }

        var role = await _roles.FindByIdAsync(SelectedRoleId);
        if (role == null || role.WorkspaceId != ws.Id)
        {
            ModelState.AddModelError(string.Empty, "Invalid role selection.");
            return await OnGetAsync(slug);
        }

        await _uwr.AddAsync(SelectedUserId, ws.Id, SelectedRoleId, uid);
        return Redirect($"/workspaces/{slug}/users/roles/assign");
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        await _uwr.RemoveAsync(userId, ws.Id, roleId);
        return Redirect($"/workspaces/{slug}/users/roles/assign");
    }
}
