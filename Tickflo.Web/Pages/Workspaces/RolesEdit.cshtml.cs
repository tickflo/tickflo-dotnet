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
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IHttpContextAccessor _http;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Role? Role { get; private set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public bool Admin { get; set; }

    public RolesEditModel(IWorkspaceRepository workspaces, IUserWorkspaceRoleRepository uwr, IRoleRepository roles, IRolePermissionRepository rolePerms, IHttpContextAccessor http)
    {
        _workspaces = workspaces;
        _uwr = uwr;
        _roles = roles;
        _rolePerms = rolePerms;
        _http = http;
    }

    // Security item bindings
    public class SectionPermissionInput
    {
        public string Section { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
        public string? TicketViewScope { get; set; } // only for tickets: "all"|"mine"|"team"
    }

    [BindProperty]
    public List<SectionPermissionInput> Permissions { get; set; } = new();

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
            // Load existing permissions
            var existingPerms = await _rolePerms.ListByRoleAsync(role.Id);
            Permissions = BuildDefaultPermissions();
            foreach (var p in existingPerms)
            {
                var dest = Permissions.FirstOrDefault(x => string.Equals(x.Section, p.Section, StringComparison.OrdinalIgnoreCase));
                if (dest != null)
                {
                    dest.CanView = p.CanView;
                    dest.CanEdit = p.CanEdit;
                    dest.CanCreate = p.CanCreate;
                    if (string.Equals(p.Section, "tickets", StringComparison.OrdinalIgnoreCase))
                        dest.TicketViewScope = string.IsNullOrWhiteSpace(p.TicketViewScope) ? "all" : p.TicketViewScope!.ToLowerInvariant();
                }
            }
        }
        else
        {
            Role = new Role { WorkspaceId = workspaceId };
            Name = string.Empty;
            Admin = false;
            Permissions = BuildDefaultPermissions();
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
            // Reload to get the created role for permissions
            var created = await _roles.FindByNameAsync(workspaceId, nameTrim);
            if (created != null)
            {
                await _rolePerms.UpsertAsync(created.Id, MapEffectivePermissions(), uid);
            }
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
            await _rolePerms.UpsertAsync(role.Id, MapEffectivePermissions(), uid);
        }
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return Redirect($"/workspaces/{slug}/roles?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
    }

    private List<SectionPermissionInput> BuildDefaultPermissions()
    {
        var sections = new[] { "dashboard", "contacts", "inventory", "locations", "reports", "roles", "teams", "tickets", "users", "settings" };
        var list = sections.Select(s => new SectionPermissionInput
        {
            Section = s,
            CanView = s == "dashboard" || s == "tickets", // sensible defaults
            CanEdit = false,
            CanCreate = false,
            TicketViewScope = s == "tickets" ? "all" : null
        }).ToList();
        return list;
    }

    private IEnumerable<Tickflo.Core.Data.EffectiveSectionPermission> MapEffectivePermissions()
    {
        return Permissions.Select(p => new Tickflo.Core.Data.EffectiveSectionPermission
        {
            Section = p.Section.ToLowerInvariant(),
            CanView = p.CanView,
            CanEdit = p.CanEdit,
            CanCreate = p.CanCreate,
            TicketViewScope = p.Section.Equals("tickets", StringComparison.OrdinalIgnoreCase) ? (p.TicketViewScope ?? "all").ToLowerInvariant() : null
        });
    }
}
