using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class LocationsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ILocationRepository _locationRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<LocationItem> Locations { get; private set; } = new();

    public LocationsModel(IWorkspaceRepository workspaceRepo, ILocationRepository locationRepo)
    {
        _workspaceRepo = workspaceRepo;
        _locationRepo = locationRepo;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        Locations = (await _locationRepo.ListAsync(Workspace!.Id))
            .Select(l => new LocationItem { Id = l.Id, Name = l.Name, Address = l.Address, Active = l.Active })
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int locationId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
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
    }
}
