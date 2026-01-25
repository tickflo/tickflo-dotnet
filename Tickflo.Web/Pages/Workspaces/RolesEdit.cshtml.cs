namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class RolesEditModel(IWorkspaceService workspaceService, IRoleRepository roleRepository, IRolePermissionRepository rolePermissionRepository, IWorkspaceRolesEditViewService workspaceRolesEditViewService) : WorkspacePageModel
{
    #region Constants
    private const int NewRoleId = 0;
    private const string TicketSection = "tickets";
    private const string DashboardSection = "dashboard";
    private const string DefaultTicketViewScope = "all";
    private const string RoleNameRequired = "Role name is required";
    private const string RoleCreationFailed = "Failed to create role.";
    private const string RoleNameDuplicate = "A role with that name already exists";
    private static readonly string[] DefaultSections = ["dashboard", "contacts", "inventory", "locations", "reports", "roles", "teams", "tickets", "users", "settings"];
    #endregion

    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IWorkspaceRolesEditViewService workspaceRolesEditViewService = workspaceRolesEditViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Role? Role { get; private set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public bool Admin { get; set; }

    public class SectionPermissionInput
    {
        public string Section { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
        public string? TicketViewScope { get; set; } // only for tickets: "all"|"mine"|"team"
    }

    [BindProperty]
    public List<SectionPermissionInput> Permissions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        var workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var workspaceId = workspace.Id;
        var data = await this.workspaceRolesEditViewService.BuildAsync(workspaceId, uid, id);
        if (!data.IsAdmin)
        {
            return this.Forbid();
        }

        if (id > 0)
        {
            this.LoadExistingRole(data, workspaceId);
        }
        else
        {
            this.InitializeNewRole(workspaceId);
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        var workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var workspaceId = workspace.Id;
        var data = await this.workspaceRolesEditViewService.BuildAsync(workspaceId, uid, id);
        if (!data.IsAdmin)
        {
            return this.Forbid();
        }

        var nameValidation = this.ValidateRoleName();
        if (nameValidation != null)
        {
            return nameValidation;
        }

        try
        {
            if (id == NewRoleId)
            {
                await this.CreateNewRoleAsync(workspaceId, uid);
            }
            else
            {
                await this.UpdateExistingRoleAsync(id, workspaceId, uid, data);
            }

            return this.RedirectToRolesWithPreservedFilters(slug);
        }
        catch (InvalidOperationException ex)
        {
            this.ModelState.AddModelError(string.Empty, ex.Message);
            return this.Page();
        }
    }

    private void LoadExistingRole(WorkspaceRolesEditViewData data, int workspaceId)
    {
        var role = data.ExistingRole;
        var roleCheck = this.EnsureEntityBelongsToWorkspace(role, workspaceId);
        if (roleCheck is not null)
        {
            throw new InvalidOperationException("Role does not belong to this workspace");
        }

        this.Role = role;
        this.Name = role!.Name ?? string.Empty;
        this.Admin = role.Admin;

        this.Permissions = BuildDefaultPermissions();
        this.ApplyExistingPermissions(data.ExistingPermissions);
    }

    private void InitializeNewRole(int workspaceId)
    {
        this.Role = new Role { WorkspaceId = workspaceId };
        this.Name = string.Empty;
        this.Admin = false;
        this.Permissions = BuildDefaultPermissions();
    }

    private PageResult? ValidateRoleName()
    {
        var nameTrim = this.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nameTrim))
        {
            this.ModelState.AddModelError(nameof(this.Name), RoleNameRequired);
            return this.Page();
        }
        return null;
    }

    private async Task CreateNewRoleAsync(int workspaceId, int userId)
    {
        var nameTrim = this.Name?.Trim() ?? string.Empty;
        var createdId = (await this.roleRepository.AddAsync(new Role
        {
            WorkspaceId = workspaceId,
            Name = nameTrim,
            Admin = this.Admin,
            CreatedBy = userId,
        }))?.Id ?? 0;

        if (createdId == 0)
        {
            this.ModelState.AddModelError(string.Empty, RoleCreationFailed);
            throw new InvalidOperationException(RoleCreationFailed);
        }

        await this.rolePermissionRepository.UpsertAsync(createdId, this.MapEffectivePermissions(), userId);
    }

    private async Task UpdateExistingRoleAsync(int id, int workspaceId, int userId, WorkspaceRolesEditViewData viewData)
    {
        var nameTrim = this.Name?.Trim() ?? string.Empty;
        var role = viewData.ExistingRole ?? await this.roleRepository.FindByIdAsync(id);

        var roleCheck = this.EnsureEntityBelongsToWorkspace(role, workspaceId);
        if (roleCheck is not null)
        {
            throw new InvalidOperationException("Role does not belong to this workspace");
        }

        var existingWithName = await this.roleRepository.FindByNameAsync(workspaceId, nameTrim);
        if (existingWithName != null && existingWithName.Id != role!.Id)
        {
            this.ModelState.AddModelError(nameof(this.Name), RoleNameDuplicate);
            this.Role = role;
            throw new InvalidOperationException(RoleNameDuplicate);
        }

        role!.Name = nameTrim;
        role.Admin = this.Admin;
        await this.roleRepository.UpdateAsync(role);
        await this.rolePermissionRepository.UpsertAsync(role.Id, this.MapEffectivePermissions(), userId);
    }

    private void ApplyExistingPermissions(IEnumerable<EffectiveSectionPermission> existingPerms)
    {
        foreach (var permission in existingPerms)
        {
            var dest = this.Permissions.FirstOrDefault(x => string.Equals(x.Section, permission.Section, StringComparison.OrdinalIgnoreCase));
            if (dest != null)
            {
                dest.CanView = permission.CanView;
                dest.CanEdit = permission.CanEdit;
                dest.CanCreate = permission.CanCreate;
                if (string.Equals(permission.Section, TicketSection, StringComparison.OrdinalIgnoreCase))
                {
                    dest.TicketViewScope = string.IsNullOrWhiteSpace(permission.TicketViewScope) ? DefaultTicketViewScope : permission.TicketViewScope.ToLowerInvariant();
                }
            }
        }
    }

    private RedirectResult RedirectToRolesWithPreservedFilters(string slug)
    {
        var queryQ = this.Request.Query["Query"].ToString();
        var pageQ = this.Request.Query["PageNumber"].ToString();
        return this.Redirect($"/workspaces/{slug}/roles?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
    }

    private static List<SectionPermissionInput> BuildDefaultPermissions()
    {
        var list = DefaultSections.Select(section => new SectionPermissionInput
        {
            Section = section,
            CanView = section is DashboardSection or TicketSection,
            CanEdit = false,
            CanCreate = false,
            TicketViewScope = section == TicketSection ? DefaultTicketViewScope : null
        }).ToList();
        return list;
    }

    private IEnumerable<EffectiveSectionPermission> MapEffectivePermissions() => this.Permissions.Select(p => new EffectiveSectionPermission
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
