using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces
{
    public class InventoryEditModel : PageModel
    {
        private readonly IInventoryRepository _inventory;
        private readonly IWorkspaceRepository _workspaces;
        private readonly ILocationRepository _locations;
        private readonly IUserWorkspaceRoleRepository _roles;
        private readonly IRolePermissionRepository _rolePerms;
        private readonly IHttpContextAccessor _http;

        public InventoryEditModel(IInventoryRepository inventory, IWorkspaceRepository workspaces, ILocationRepository locations, IUserWorkspaceRoleRepository roles, IHttpContextAccessor http, IRolePermissionRepository rolePerms)
        {
            _inventory = inventory;
            _workspaces = workspaces;
            _locations = locations;
            _roles = roles;
            _http = http;
            _rolePerms = rolePerms;
        }
    public bool CanViewInventory { get; private set; }
    public bool CanEditInventory { get; private set; }
    public bool CanCreateInventory { get; private set; }

        public string WorkspaceSlug { get; private set; } = string.Empty;
        public Workspace? Workspace { get; set; }
        public List<Location> LocationOptions { get; set; } = new();

        [BindProperty]
        public Inventory Item { get; set; } = new Inventory();

        public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null) return NotFound();
            var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var workspaceId = Workspace.Id;
            var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, workspaceId);
            var eff = int.TryParse(uidStr, out var currentUserId) ? await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, currentUserId) : new Dictionary<string, EffectiveSectionPermission>();
            if (isAdmin)
            {
                CanViewInventory = CanEditInventory = CanCreateInventory = true;
            }
            else if (eff.TryGetValue("inventory", out var ip))
            {
                CanViewInventory = ip.CanView;
                CanEditInventory = ip.CanEdit;
                CanCreateInventory = ip.CanCreate;
            }
            if (!CanViewInventory) return Forbid();
            if (id > 0)
            {
                var existing = await _inventory.FindAsync(workspaceId, id);
                if (existing == null) return NotFound();
                Item = existing;
            }
            else
            {
                Item = new Inventory { WorkspaceId = workspaceId, Status = "active" };
            }
            LocationOptions = (await _locations.ListAsync(workspaceId)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null) return NotFound();
            var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var workspaceId = Workspace.Id;
            var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, workspaceId);
            var eff = int.TryParse(uidStr, out var currentUserId) ? await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, currentUserId) : new Dictionary<string, EffectiveSectionPermission>();
            bool allowed = isAdmin;
            if (!allowed && eff.TryGetValue("inventory", out var ip))
            {
                allowed = (id == 0) ? ip.CanCreate : ip.CanEdit;
            }
            if (!allowed) return Forbid();

            if (!ModelState.IsValid)
            {
                LocationOptions = (await _locations.ListAsync(workspaceId)).ToList();
                return Page();
            }
            Item.WorkspaceId = workspaceId;
            if (id == 0)
            {
                await _inventory.CreateAsync(Item);
            }
            else
            {
                Item.Id = id;
                await _inventory.UpdateAsync(Item);
            }
            var queryQ = Request.Query["Query"].ToString();
            var locationQ = Request.Query["LocationId"].ToString();
            var pageQ = Request.Query["PageNumber"].ToString();
            var slugSafe = WorkspaceSlug;
            return Redirect($"/workspaces/{slugSafe}/inventory?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}&LocationId={Uri.EscapeDataString(locationQ ?? string.Empty)}&PageNumber={Uri.EscapeDataString(pageQ ?? string.Empty)}");
        }
    }
}
