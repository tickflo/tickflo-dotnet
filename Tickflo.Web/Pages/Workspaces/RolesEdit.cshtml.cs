using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Tickflo.Web.Pages.Workspaces;

public class RolesEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly IRoleRepository _roles;
    private readonly IHttpContextAccessor _http;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Role? Role { get; private set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public bool Admin { get; set; }

    public RolesEditModel(IWorkspaceRepository workspaces, IUserWorkspaceRoleRepository uwr, IRoleRepository roles, IHttpContextAccessor http)
    {
        _workspaces = workspaces;
        _uwr = uwr;
        _roles = roles;
        _http = http;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        var role = await _roles.FindByIdAsync(id);
        if (role == null || role.WorkspaceId != ws.Id) return NotFound();
        Role = role;
        Name = role.Name;
        Admin = role.Admin;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _uwr.IsAdminAsync(uid, ws.Id);
        if (!isAdmin) return Forbid();

        var role = await _roles.FindByIdAsync(id);
        if (role == null || role.WorkspaceId != ws.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Role name is required");
            Role = role;
            return Page();
        }

        // ensure unique name per workspace
        var existing = await _roles.FindByNameAsync(ws.Id, Name);
        if (existing != null && existing.Id != role.Id)
        {
            ModelState.AddModelError(nameof(Name), "A role with that name already exists");
            Role = role;
            return Page();
        }

        role.Name = Name;
        role.Admin = Admin;
        await _roles.UpdateAsync(role);
        return Redirect($"/workspaces/{slug}/roles");
    }
}
