namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class RolePermissionRepository(TickfloDbContext db, IUserWorkspaceRoleRepository uwrRepo) : IRolePermissionRepository
{
    private readonly TickfloDbContext _db = db;
    private readonly IUserWorkspaceRoleRepository _uwrRepo = uwrRepo;

    private static readonly string[] ManagedSections = ["dashboard", "contacts", "inventory", "locations", "reports", "roles", "teams", "tickets", "users", "settings"];

    public async Task<List<EffectiveSectionPermission>> ListByRoleAsync(int roleId)
    {
        // Join role_permissions -> permissions and aggregate by resource
        var links = await this._db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        var permIds = links.Select(l => l.PermissionId).Distinct().ToList();
        var perms = await this._db.Permissions.Where(p => permIds.Contains(p.Id)).ToListAsync();
        var result = new List<EffectiveSectionPermission>();
        foreach (var section in ManagedSections)
        {
            var eff = new EffectiveSectionPermission { Section = section };
            var view = perms.Any(p => p.Resource.Equals(section, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("view", StringComparison.OrdinalIgnoreCase));
            var edit = perms.Any(p => p.Resource.Equals(section, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("edit", StringComparison.OrdinalIgnoreCase));
            var create = perms.Any(p => p.Resource.Equals(section, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("create", StringComparison.OrdinalIgnoreCase));
            eff.CanView = view;
            eff.CanEdit = edit;
            eff.CanCreate = create;
            if (section.Equals("tickets", StringComparison.OrdinalIgnoreCase))
            {
                var scopes = perms.Where(p => p.Resource.Equals("tickets_scope", StringComparison.OrdinalIgnoreCase)).Select(p => p.Action.ToLowerInvariant()).ToList();
                eff.TicketViewScope = scopes.Contains("mine") ? "mine" : scopes.Contains("team") ? "team" : scopes.Contains("all") ? "all" : "all";
            }
            result.Add(eff);
        }
        return result;
    }

    public async Task UpsertAsync(int roleId, IEnumerable<EffectiveSectionPermission> permissions, int? actorUserId = null)
    {
        // Ensure permission catalog rows exist, then set role links accordingly.
        var catalog = await this._db.Permissions.ToListAsync();
        var actor = actorUserId;

        // Build desired permission set for this role
        var desired = new List<int>();
        foreach (var p in permissions)
        {
            var section = (p.Section ?? string.Empty).ToLowerInvariant();
            if (!ManagedSections.Contains(section))
            {
                continue;
            }

            if (p.CanView)
            {
                desired.Add(await this.EnsurePermissionIdAsync(catalog, section, "view"));
            }

            if (p.CanEdit)
            {
                desired.Add(await this.EnsurePermissionIdAsync(catalog, section, "edit"));
            }

            if (p.CanCreate)
            {
                desired.Add(await this.EnsurePermissionIdAsync(catalog, section, "create"));
            }

            if (section == "tickets")
            {
                var scope = string.IsNullOrWhiteSpace(p.TicketViewScope) ? "all" : p.TicketViewScope!.ToLowerInvariant();
                // choose only one scope; store desired scope
                desired.Add(await this.EnsurePermissionIdAsync(catalog, "tickets_scope", scope));
            }
        }

        // Current links for managed resources
        var currentLinks = await this._db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        var currentPerms = await this._db.Permissions.Where(p => currentLinks.Select(cl => cl.PermissionId).Contains(p.Id)).ToListAsync();

        // Filter to links related to our managed resources to avoid touching unrelated permissions
        var managedCurrentLinkIds = currentLinks
            .Where(l => currentPerms.Any(p => ManagedSections.Contains(p.Resource.ToLowerInvariant()) || p.Resource.Equals("tickets_scope", StringComparison.OrdinalIgnoreCase)))
            .Select(l => l.PermissionId)
            .ToHashSet();

        var currentSet = managedCurrentLinkIds;
        var desiredSet = desired.ToHashSet();

        // Remove obsolete links
        var toRemove = currentLinks.Where(l => managedCurrentLinkIds.Contains(l.PermissionId) && !desiredSet.Contains(l.PermissionId)).ToList();
        if (toRemove.Count > 0)
        {
            this._db.RolePermissions.RemoveRange(toRemove);
        }

        // Add new links
        var toAdd = desiredSet.Except(currentSet).ToList();
        foreach (var pid in toAdd)
        {
            this._db.RolePermissions.Add(new RolePermissionLink
            {
                RoleId = roleId,
                PermissionId = pid,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actor
            });
        }

        await this._db.SaveChangesAsync();
    }

    private async Task<int> EnsurePermissionIdAsync(List<Permission> catalog, string resource, string action)
    {
        var found = catalog.FirstOrDefault(p => p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase) && p.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            return found.Id;
        }

        var p = new Permission { Resource = resource, Action = action };
        this._db.Permissions.Add(p);
        await this._db.SaveChangesAsync();
        catalog.Add(p);
        return p.Id;
    }

    public async Task<Dictionary<string, EffectiveSectionPermission>> GetEffectivePermissionsForUserAsync(int workspaceId, int userId)
    {
        var roles = await this._uwrRepo.GetRolesAsync(userId, workspaceId);
        var roleIds = roles.Select(r => r.Id).ToList();
        var isAdmin = roles.Any(r => r.Admin);
        var result = new Dictionary<string, EffectiveSectionPermission>(StringComparer.OrdinalIgnoreCase);
        if (isAdmin)
        {
            foreach (var s in ManagedSections)
            {
                result[s] = new EffectiveSectionPermission { Section = s, CanView = true, CanEdit = true, CanCreate = true, TicketViewScope = s == "tickets" ? "all" : null };
            }
            return result;
        }
        var links = await this._db.RolePermissions.Where(rp => roleIds.Contains(rp.RoleId)).ToListAsync();
        var permIds = links.Select(l => l.PermissionId).Distinct().ToList();
        var perms = await this._db.Permissions.Where(p => permIds.Contains(p.Id)).ToListAsync();
        foreach (var s in ManagedSections)
        {
            var eff = new EffectiveSectionPermission
            {
                Section = s,
                CanView = perms.Any(p => p.Resource.Equals(s, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("view", StringComparison.OrdinalIgnoreCase)),
                CanEdit = perms.Any(p => p.Resource.Equals(s, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("edit", StringComparison.OrdinalIgnoreCase)),
                CanCreate = perms.Any(p => p.Resource.Equals(s, StringComparison.OrdinalIgnoreCase) && p.Action.Equals("create", StringComparison.OrdinalIgnoreCase))
            };
            if (s == "tickets")
            {
                var scopes = perms.Where(p => p.Resource.Equals("tickets_scope", StringComparison.OrdinalIgnoreCase)).Select(p => p.Action.ToLowerInvariant()).ToList();
                eff.TicketViewScope = scopes.Contains("mine") ? "mine" : scopes.Contains("team") ? "team" : "all";
            }
            result[s] = eff;
        }
        return result;
    }

    public async Task<string> GetTicketViewScopeForUserAsync(int workspaceId, int userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return "all";
        }

        var roles = await this._uwrRepo.GetRolesAsync(userId, workspaceId);
        var roleIds = roles.Select(r => r.Id).ToList();
        var links = await this._db.RolePermissions.Where(rp => roleIds.Contains(rp.RoleId)).ToListAsync();
        var permIds = links.Select(l => l.PermissionId).Distinct().ToList();
        var scopes = await this._db.Permissions
            .Where(p => permIds.Contains(p.Id) && p.Resource == "tickets_scope")
            .Select(p => p.Action.ToLowerInvariant())
            .ToListAsync();
        if (scopes.Contains("mine"))
        {
            return "mine";
        }

        if (scopes.Contains("team"))
        {
            return "team";
        }

        return "all";
    }
}
