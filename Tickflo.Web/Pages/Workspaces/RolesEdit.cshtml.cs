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

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = ws.Id;
        var isAdmin = await _uwr.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();
        if (id > 0)
        {
            var role = await _roles.FindByIdAsync(id);
            if (role == null || role.WorkspaceId != workspaceId) return NotFound();
            Role = role;
            Name = role.Name ?? string.Empty;
            Admin = role.Admin;
        }
        else
        {
            Role = new Role { WorkspaceId = workspaceId };
            Name = string.Empty;
            Admin = false;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaces.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = ws.Id;
        var isAdmin = await _uwr.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();
        var nameTrim = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ModelState.AddModelError(nameof(Name), "Role name is required");
            return Page();
        }
        // ensure unique name per workspace
        var existing = await _roles.FindByNameAsync(workspaceId, nameTrim);
        if (id == 0)
        {
            if (existing != null)
            {
                ModelState.AddModelError(nameof(Name), "A role with that name already exists");
                return Page();
            }
            await _roles.AddAsync(workspaceId, nameTrim, Admin, uid);
        }
        else
        {
            var role = await _roles.FindByIdAsync(id);
            if (role == null || role.WorkspaceId != workspaceId) return NotFound();
            if (existing != null && existing.Id != role.Id)
            {
                ModelState.AddModelError(nameof(Name), "A role with that name already exists");
                Role = role;
                return Page();
            }
            role.Name = nameTrim;
            role.Admin = Admin;
            await _roles.UpdateAsync(role);
        }
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return Redirect($"/workspaces/{slug}/roles?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
    }
}
