using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

 [Authorize]
public class LocationsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IWorkspaceLocationsEditViewService _viewService;
    private readonly ILocationService _locationService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int LocationId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string Address { get; set; } = string.Empty;
    [BindProperty]
    public bool Active { get; set; } = true;
    [BindProperty]
    public int? DefaultAssigneeUserId { get; set; }
    public List<User> MemberOptions { get; private set; } = new();
    [BindProperty]
    public List<int> SelectedContactIds { get; set; } = new();
    public List<Contact> ContactOptions { get; private set; } = new();

    public LocationsEditModel(IWorkspaceRepository workspaceRepo, ILocationRepository locationRepo, IWorkspaceLocationsEditViewService viewService, ILocationService locationService)
    {
        _workspaceRepo = workspaceRepo;
        _locationRepo = locationRepo;
        _viewService = viewService;
        _locationService = locationService;
    }
    public bool CanViewLocations { get; private set; }
    public bool CanEditLocations { get; private set; }
    public bool CanCreateLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int locationId = 0)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, locationId);
        CanViewLocations = viewData.CanViewLocations;
        CanEditLocations = viewData.CanEditLocations;
        CanCreateLocations = viewData.CanCreateLocations;
        
        if (!CanViewLocations) return Forbid();

        MemberOptions = viewData.MemberOptions;
        ContactOptions = viewData.ContactOptions;
        
        if (locationId > 0)
        {
            if (viewData.ExistingLocation == null) return NotFound();
            LocationId = viewData.ExistingLocation.Id;
            Name = viewData.ExistingLocation.Name ?? string.Empty;
            Address = viewData.ExistingLocation.Address ?? string.Empty;
            Active = viewData.ExistingLocation.Active;
            DefaultAssigneeUserId = viewData.ExistingLocation.DefaultAssigneeUserId;
            SelectedContactIds = viewData.SelectedContactIds;
        }
        else
        {
            LocationId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Active = true;
            DefaultAssigneeUserId = null;
            SelectedContactIds = new();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, LocationId);
        if (LocationId == 0 && !viewData.CanCreateLocations) return Forbid();
        if (LocationId > 0 && !viewData.CanEditLocations) return Forbid();
        
        if (!ModelState.IsValid) return Page();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var addressTrim = Address?.Trim() ?? string.Empty;
        int effectiveLocationId = LocationId;
        try
        {
            if (LocationId == 0)
            {
                var created = await _locationService.CreateLocationAsync(workspaceId, new CreateLocationRequest
                {
                    Name = nameTrim,
                    Address = addressTrim,
                    DefaultAssigneeUserId = DefaultAssigneeUserId
                });
                // Persist Active flag if different from default
                created.Active = Active;
                await _locationRepo.UpdateAsync(created);
                effectiveLocationId = created.Id;
                TempData["Success"] = $"Location '{created.Name}' created successfully.";
            }
            else
            {
                var updated = await _locationService.UpdateLocationAsync(workspaceId, LocationId, new UpdateLocationRequest
                {
                    Name = nameTrim,
                    Address = addressTrim,
                    DefaultAssigneeUserId = DefaultAssigneeUserId
                });
                // Persist Active flag
                updated.Active = Active;
                await _locationRepo.UpdateAsync(updated);
                effectiveLocationId = updated.Id;
                TempData["Success"] = $"Location '{updated.Name}' updated successfully.";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return Page();
        }
        // Persist contact assignments
        await _locationRepo.SetContactsAsync(workspaceId, effectiveLocationId, SelectedContactIds ?? new List<int>());
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
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
