using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces
{
    [Authorize]
    public class InventoryModel : PageModel
    {
        private readonly IInventoryRepository _inventory;
        private readonly IWorkspaceRepository _workspaces;
        private readonly ICurrentUserService _currentUserService;
        private readonly IWorkspaceAccessService _workspaceAccessService;

        public InventoryModel(
            IInventoryRepository inventory,
            IWorkspaceRepository workspaces,
            ICurrentUserService currentUserService,
            IWorkspaceAccessService workspaceAccessService)
        {
            _inventory = inventory;
            _workspaces = workspaces;
            _currentUserService = currentUserService;
            _workspaceAccessService = workspaceAccessService;
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

            if (!_currentUserService.TryGetUserId(User, out var uid))
            {
                return Forbid();
            }

            // Use service to check admin status
            IsWorkspaceAdmin = await _workspaceAccessService.UserIsWorkspaceAdminAsync(uid, Workspace.Id);
            
            // Use service to get permissions
            var permissions = await _workspaceAccessService.GetUserPermissionsAsync(Workspace.Id, uid);
            if (permissions.TryGetValue("inventory", out var ip))
            {
                CanCreateInventory = ip.CanCreate || IsWorkspaceAdmin;
                CanEditInventory = ip.CanEdit || IsWorkspaceAdmin;
            }
            else
            {
                CanCreateInventory = IsWorkspaceAdmin;
                CanEditInventory = IsWorkspaceAdmin;
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

            if (!_currentUserService.TryGetUserId(User, out var uid))
            {
                return Forbid();
            }

            // Use service to check permissions
            bool allowed = await _workspaceAccessService.CanUserPerformActionAsync(Workspace.Id, uid, "inventory", "edit");
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

            if (!_currentUserService.TryGetUserId(User, out var uid))
            {
                return Forbid();
            }

            // Use service to check permissions
            bool allowed = await _workspaceAccessService.CanUserPerformActionAsync(Workspace.Id, uid, "inventory", "edit");
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
    }
}
 
