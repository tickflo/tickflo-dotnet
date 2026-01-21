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
    #region Constants
    private const int NewRoleId = 0;
    private const string TicketSection = "tickets";
    private const string DashboardSection = "dashboard";
    private const string DefaultTicketViewScope = "all";
    private const string RoleNameRequired = "Role name is required";
    private const string RoleCreationFailed = "Failed to create role.";
    private const string RoleNameDuplicate = "A role with that name already exists";
    private static readonly string[] DefaultSections = { "dashboard", "contacts", "inventory", "locations", "reports", "roles", "teams", "tickets", "users", "settings" };
    #endregion

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

        var workspaceId = ws!.Id;
        var data = await _rolesEditViewService.BuildAsync(workspaceId, uid, id);
        if (!data.IsAdmin) return Forbid();

        if (id > 0)
            LoadExistingRole(data, workspaceId);
        else
            InitializeNewRole(workspaceId);

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

        var nameValidation = ValidateRoleName();
        if (nameValidation != null) return nameValidation;

        try
        {
            if (id == NewRoleId)
                await CreateNewRoleAsync(workspaceId, uid);
            else
                await UpdateExistingRoleAsync(id, workspaceId, uid, data);

            return RedirectToRolesWithPreservedFilters(slug);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private void LoadExistingRole(WorkspaceRolesEditViewData data, int workspaceId)
    {
        var role = data.ExistingRole;
        var roleCheck = EnsureEntityBelongsToWorkspace(role, workspaceId);
        if (roleCheck is not null) throw new InvalidOperationException("Role does not belong to this workspace");

        Role = role;
        Name = role!.Name ?? string.Empty;
        Admin = role.Admin;

        Permissions = BuildDefaultPermissions();
        ApplyExistingPermissions(data.ExistingPermissions);
    }

    private void InitializeNewRole(int workspaceId)
    {
        Role = new Role { WorkspaceId = workspaceId };
        Name = string.Empty;
        Admin = false;
        Permissions = BuildDefaultPermissions();
    }

    private IActionResult? ValidateRoleName()
    {
        var nameTrim = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            ModelState.AddModelError(nameof(Name), RoleNameRequired);
            return Page();
        }
        return null;
    }

    private async Task CreateNewRoleAsync(int workspaceId, int userId)
    {
        var nameTrim = Name?.Trim() ?? string.Empty;
        var createdId = (await _roles.AddAsync(workspaceId, nameTrim, Admin, userId))?.Id ?? 0;

        if (createdId == 0)
        {
            ModelState.AddModelError(string.Empty, RoleCreationFailed);
            throw new InvalidOperationException(RoleCreationFailed);
        }

        await _rolePerms.UpsertAsync(createdId, MapEffectivePermissions(), userId);
    }

    private async Task UpdateExistingRoleAsync(int id, int workspaceId, int userId, WorkspaceRolesEditViewData viewData)
    {
        var nameTrim = Name?.Trim() ?? string.Empty;
        var role = viewData.ExistingRole ?? await _roles.FindByIdAsync(id);

        var roleCheck = EnsureEntityBelongsToWorkspace(role, workspaceId);
        if (roleCheck is not null) throw new InvalidOperationException("Role does not belong to this workspace");

        var existingWithName = await _roles.FindByNameAsync(workspaceId, nameTrim);
        if (existingWithName != null && existingWithName.Id != role!.Id)
        {
            ModelState.AddModelError(nameof(Name), RoleNameDuplicate);
            Role = role;
            throw new InvalidOperationException(RoleNameDuplicate);
        }

        role!.Name = nameTrim;
        role.Admin = Admin;
        await _roles.UpdateAsync(role);
        await _rolePerms.UpsertAsync(role.Id, MapEffectivePermissions(), userId);
    }

    private void ApplyExistingPermissions(IEnumerable<EffectiveSectionPermission> existingPerms)
    {
        foreach (var p in existingPerms)
        {
            var dest = Permissions.FirstOrDefault(x => string.Equals(x.Section, p.Section, StringComparison.OrdinalIgnoreCase));
            if (dest != null)
            {
                dest.CanView = p.CanView;
                dest.CanEdit = p.CanEdit;
                dest.CanCreate = p.CanCreate;
                if (string.Equals(p.Section, TicketSection, StringComparison.OrdinalIgnoreCase))
                    dest.TicketViewScope = string.IsNullOrWhiteSpace(p.TicketViewScope) ? DefaultTicketViewScope : p.TicketViewScope!.ToLowerInvariant();
            }
        }
    }

    private RedirectResult RedirectToRolesWithPreservedFilters(string slug)
    {
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return Redirect($"/workspaces/{slug}/roles?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
    }

    private List<SectionPermissionInput> BuildDefaultPermissions()
    {
        var list = DefaultSections.Select(s => new SectionPermissionInput
        {
            Section = s,
            CanView = s == DashboardSection || s == TicketSection,
            CanEdit = false,
            CanCreate = false,
            TicketViewScope = s == TicketSection ? DefaultTicketViewScope : null
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
            TicketViewScope = p.Section.Equals(TicketSection, StringComparison.OrdinalIgnoreCase) 
                ? (p.TicketViewScope ?? DefaultTicketViewScope).ToLowerInvariant() 
                : null
        });
    }


}

