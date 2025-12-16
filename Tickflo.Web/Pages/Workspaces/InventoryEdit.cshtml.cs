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
        private readonly IHttpContextAccessor _http;

        public InventoryEditModel(IInventoryRepository inventory, IWorkspaceRepository workspaces, ILocationRepository locations, IUserWorkspaceRoleRepository roles, IHttpContextAccessor http)
        {
            _inventory = inventory;
            _workspaces = workspaces;
            _locations = locations;
            _roles = roles;
            _http = http;
        }

        public string WorkspaceSlug { get; private set; } = string.Empty;
        public Workspace? Workspace { get; set; }
        public List<Location> LocationOptions { get; set; } = new();

        [BindProperty]
        public Inventory Item { get; set; } = new Inventory();

        public async Task<IActionResult> OnGetAsync(string slug, int id)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null) return NotFound();
            var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
            if (!isAdmin) return Forbid();

            var existing = await _inventory.FindAsync(Workspace.Id, id);
            if (existing == null) return NotFound();
            Item = existing;
            LocationOptions = (await _locations.ListAsync(Workspace.Id)).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string slug, int id)
        {
            WorkspaceSlug = slug;
            Workspace = await _workspaces.FindBySlugAsync(slug);
            if (Workspace == null) return NotFound();
            var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
            if (!isAdmin) return Forbid();

            if (!ModelState.IsValid)
            {
                LocationOptions = (await _locations.ListAsync(Workspace.Id)).ToList();
                return Page();
            }
            Item.WorkspaceId = Workspace.Id;
            Item.Id = id;
            await _inventory.UpdateAsync(Item);
            return Redirect($"/workspaces/{Workspace.Slug}/inventory");
        }
    }
}
