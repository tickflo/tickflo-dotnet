using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class LocationsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IRolePermissionRepository _rolePerms;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<LocationItem> Locations { get; private set; } = new();

    public LocationsModel(IWorkspaceRepository workspaceRepo, ILocationRepository locationRepo, IRolePermissionRepository rolePerms)
    {
        _workspaceRepo = workspaceRepo;
        _locationRepo = locationRepo;
        _rolePerms = rolePerms;
    }
    public bool CanCreateLocations { get; private set; }
    public bool CanEditLocations { get; private set; }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Workspace != null && int.TryParse(uidStr, out var uid))
        {
            var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, uid);
            if (eff.TryGetValue("locations", out var lp))
            {
                CanCreateLocations = lp.CanCreate;
                CanEditLocations = lp.CanEdit;
            }
        }
        var list = await _locationRepo.ListAsync(Workspace!.Id);
        var items = new List<LocationItem>();
        foreach (var l in list)
        {
            var ids = await _locationRepo.ListContactIdsAsync(Workspace.Id, l.Id);
            var count = ids.Count;
            var previewNames = await _locationRepo.ListContactNamesAsync(Workspace.Id, l.Id, 3);
            var preview = string.Join(", ", previewNames);
            items.Add(new LocationItem { Id = l.Id, Name = l.Name, Address = l.Address, Active = l.Active, ContactCount = count, ContactPreview = preview });
        }
        Locations = items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int locationId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        // Enforce edit permission for deletes
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, uid);
        bool allowed = eff.TryGetValue("locations", out var lp) && lp.CanEdit;
        if (!allowed) return Forbid();
        var ok = await _locationRepo.DeleteAsync(Workspace.Id, locationId);
        TempData["Success"] = ok ? $"Location #{locationId} deleted." : "Location not found.";
        return RedirectToPage("/Workspaces/Locations", new { slug });
    }

    public record LocationItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int ContactCount { get; set; }
        public string ContactPreview { get; set; } = string.Empty;
    }
}
