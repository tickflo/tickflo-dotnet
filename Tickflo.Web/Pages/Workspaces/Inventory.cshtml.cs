using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using System.Security.Claims;

namespace Tickflo.Web.Pages.Workspaces
{
    [Authorize]
    public class InventoryModel : PageModel
    {
        private readonly IInventoryRepository _inventory;
        private readonly IWorkspaceRepository _workspaces;
        private readonly IUserWorkspaceRoleRepository _roles;
        private readonly IRolePermissionRepository _rolePerms;

        public InventoryModel(IInventoryRepository inventory, IWorkspaceRepository workspaces, IUserWorkspaceRoleRepository roles, IRolePermissionRepository rolePerms)
        {
            _inventory = inventory;
            _workspaces = workspaces;
            _roles = roles;
            _rolePerms = rolePerms;
        }

        [BindProperty(SupportsGet = true)]
        public string? Query { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        public string WorkspaceSlug { get; private set; } = string.Empty;
        public bool IsWorkspaceAdmin { get; private set; }
        public bool CanCreateInventory { get; private set; }
        public bool CanEditInventory { get; private set; }
        public Workspace? Workspace { get; set; }
        public IEnumerable<Inventory> Items { get; set; } = new List<Inventory>();

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null)
            {
                return NotFound();
            }
            IsWorkspaceAdmin = TryGetUserId(out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
            if (TryGetUserId(out var currentUserId))
            {
                var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, currentUserId);
                if (eff.TryGetValue("inventory", out var ip))
                {
                    CanCreateInventory = ip.CanCreate || IsWorkspaceAdmin;
                    CanEditInventory = ip.CanEdit || IsWorkspaceAdmin;
                }
                else
                {
                    CanCreateInventory = IsWorkspaceAdmin;
                    CanEditInventory = IsWorkspaceAdmin;
                }
            }
            Items = await _inventory.ListAsync(Workspace.Id, Query, Status);
            return Page();
        }

        public async Task<IActionResult> OnPostArchiveAsync(string slug, int id)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null)
            {
                return NotFound();
            }
            var isAdmin = TryGetUserId(out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
            bool allowed = isAdmin;
            if (!allowed && TryGetUserId(out var currentUserId))
            {
                var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, currentUserId);
                if (eff.TryGetValue("inventory", out var ip)) allowed = ip.CanEdit;
            }
            if (!allowed)
            {
                return Forbid();
            }
            var item = await _inventory.FindAsync(Workspace.Id, id);
            if (item == null)
            {
                return NotFound();
            }
            item.Status = "archived";
            await _inventory.UpdateAsync(item);
            return Redirect($"/workspaces/{Workspace.Slug}/inventory");
        }

        public async Task<IActionResult> OnPostRestoreAsync(string slug, int id)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null)
            {
                return NotFound();
            }
            var isAdmin = TryGetUserId(out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
            bool allowed = isAdmin;
            if (!allowed && TryGetUserId(out var currentUserId))
            {
                var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, currentUserId);
                if (eff.TryGetValue("inventory", out var ip)) allowed = ip.CanEdit;
            }
            if (!allowed)
            {
                return Forbid();
            }
            var item = await _inventory.FindAsync(Workspace.Id, id);
            if (item == null)
            {
                return NotFound();
            }
            item.Status = "active";
            await _inventory.UpdateAsync(item);
            return Redirect($"/workspaces/{Workspace.Slug}/inventory");
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
    }
}
 
