using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class RolesEditModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly IRoleRepository _roles;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IRoleManagementService _roleService;
    private readonly IWorkspaceRolesEditViewService _rolesEditViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Role? Role { get; private set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public bool Admin { get; set; }

    public RolesEditModel(IWorkspaceRepository workspaces, IRoleRepository roles, IRolePermissionRepository rolePerms, IRoleManagementService roleService, IWorkspaceRolesEditViewService rolesEditViewService)
    {
        _workspaces = workspaces;
        _roles = roles;
        _rolePerms = rolePerms;
        _roleService = roleService;
        _rolesEditViewService = rolesEditViewService;
    }

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
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = ws.Id;
        var data = await _rolesEditViewService.BuildAsync(workspaceId, uid, id);
        if (!data.IsAdmin) return Forbid();
        if (id > 0)
        {
            var role = data.ExistingRole;
            var roleCheck = EnsureEntityBelongsToWorkspace(role, workspaceId);
            if (roleCheck is not null) return roleCheck;
            Role = role;
            Name = role.Name ?? string.Empty;
            Admin = role.Admin;
            // Load existing permissions from view data
            var existingPerms = data.ExistingPermissions;
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
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = ws.Id;
        var data = await _rolesEditViewService.BuildAsync(workspaceId, uid, id);
        if (!data.IsAdmin) return Forbid();
        var nameTrim = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ModelState.AddModelError(nameof(Name), "Role name is required");
            return Page();
        }
        try
        {
            if (id == 0)
            {
                // Create role
                var createdId = (await _roles.AddAsync(workspaceId, nameTrim, Admin, uid))?.Id ?? 0;
                if (createdId == 0)
                {
                    ModelState.AddModelError(string.Empty, "Failed to create role.");
                    return Page();
                }
                await _rolePerms.UpsertAsync(createdId, MapEffectivePermissions(), uid);
            }
            else
            {
                var role = data.ExistingRole ?? await _roles.FindByIdAsync(id);
                var roleCheck = EnsureEntityBelongsToWorkspace(role, workspaceId);
                if (roleCheck is not null) return roleCheck;
                // Ensure name uniqueness for update
                var existing = await _roles.FindByNameAsync(workspaceId, nameTrim);
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
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
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

