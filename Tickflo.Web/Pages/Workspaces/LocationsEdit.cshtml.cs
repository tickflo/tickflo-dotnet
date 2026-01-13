using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Locations;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class LocationsEditModel : WorkspacePageModel
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
        
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;
        var workspaceId = workspace.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, locationId);
        CanViewLocations = viewData.CanViewLocations;
        CanEditLocations = viewData.CanEditLocations;
        CanCreateLocations = viewData.CanCreateLocations;
        
        if (EnsurePermissionOrForbid(CanViewLocations) is IActionResult permCheck) return permCheck;

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
        
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;
        var workspaceId = workspace.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, LocationId);
        if (EnsureCreateOrEditPermission(LocationId, viewData.CanCreateLocations, viewData.CanEditLocations) is IActionResult permCheck) return permCheck;
        
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
                SetSuccessMessage($"Location '{created.Name}' created successfully.");
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
                SetSuccessMessage($"Location '{updated.Name}' updated successfully.");
            }
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
            return Page();
        }
        // Persist contact assignments
        await _locationRepo.SetContactsAsync(workspaceId, effectiveLocationId, SelectedContactIds ?? new List<int>());
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}

