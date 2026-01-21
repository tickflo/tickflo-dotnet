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
    #region Constants
    private const int NewLocationId = 0;
    private const string LocationCreatedSuccessfully = "Location '{0}' created successfully.";
    private const string LocationUpdatedSuccessfully = "Location '{0}' updated successfully.";
    #endregion

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IWorkspaceLocationsEditViewService _viewService;
    private readonly ILocationSetupService _locationSetupService;
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

    public LocationsEditModel(
        IWorkspaceRepository workspaceRepo, 
        IUserWorkspaceRepository userWorkspaceRepo,
        ILocationRepository locationRepo, 
        IWorkspaceLocationsEditViewService viewService, 
        ILocationSetupService locationSetupService)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _locationRepo = locationRepo;
        _viewService = viewService;
        _locationSetupService = locationSetupService;
    }
    public bool CanViewLocations { get; private set; }
    public bool CanEditLocations { get; private set; }
    public bool CanCreateLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int locationId = 0)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;
        var workspaceId = workspace!.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, locationId);
        CanViewLocations = viewData.CanViewLocations;
        CanEditLocations = viewData.CanEditLocations;
        CanCreateLocations = viewData.CanCreateLocations;
        
        if (EnsurePermissionOrForbid(CanViewLocations) is IActionResult permCheck) return permCheck;

        MemberOptions = viewData.MemberOptions;
        ContactOptions = viewData.ContactOptions;
        
        if (locationId > NewLocationId)
            LoadExistingLocationData(viewData);
        else
            InitializeNewLocationForm();
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;
        var workspaceId = workspace!.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, LocationId);
        if (EnsureCreateOrEditPermission(LocationId, viewData.CanCreateLocations, viewData.CanEditLocations) is IActionResult permCheck) return permCheck;
        
        if (!ModelState.IsValid) return Page();

        int effectiveLocationId = LocationId;
        try
        {
            if (LocationId == NewLocationId)
                effectiveLocationId = await CreateAndSaveLocationAsync(workspaceId, uid);
            else
                effectiveLocationId = await UpdateAndSaveLocationAsync(workspaceId, uid);
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
            return Page();
        }
        
        await _locationRepo.SetContactsAsync(workspaceId, effectiveLocationId, SelectedContactIds ?? new List<int>());
        return RedirectToLocationsWithPreservedFilters(slug);
    }

    private void LoadExistingLocationData(WorkspaceLocationsEditViewData viewData)
    {
        if (viewData.ExistingLocation == null) return;
        
        LocationId = viewData.ExistingLocation.Id;
        Name = viewData.ExistingLocation.Name ?? string.Empty;
        Address = viewData.ExistingLocation.Address ?? string.Empty;
        Active = viewData.ExistingLocation.Active;
        DefaultAssigneeUserId = viewData.ExistingLocation.DefaultAssigneeUserId;
        SelectedContactIds = viewData.SelectedContactIds;
    }

    private void InitializeNewLocationForm()
    {
        LocationId = NewLocationId;
        Name = string.Empty;
        Address = string.Empty;
        Active = true;
        DefaultAssigneeUserId = null;
        SelectedContactIds = new();
    }

    private async Task<int> CreateAndSaveLocationAsync(int workspaceId, int userId)
    {
        var created = await _locationSetupService.CreateLocationAsync(workspaceId, new LocationCreationRequest
        {
            Name = Name?.Trim() ?? string.Empty,
            Address = Address?.Trim() ?? string.Empty
        }, userId);
        
        ApplyLocationSettings(created);
        await _locationRepo.UpdateAsync(created);
        SetSuccessMessage(string.Format(LocationCreatedSuccessfully, created.Name));
        
        return created.Id;
    }

    private async Task<int> UpdateAndSaveLocationAsync(int workspaceId, int userId)
    {
        var updated = await _locationSetupService.UpdateLocationDetailsAsync(workspaceId, LocationId, new LocationUpdateRequest
        {
            Name = Name?.Trim() ?? string.Empty,
            Address = Address?.Trim() ?? string.Empty
        }, userId);
        
        ApplyLocationSettings(updated);
        await _locationRepo.UpdateAsync(updated);
        SetSuccessMessage(string.Format(LocationUpdatedSuccessfully, updated.Name));
        
        return updated.Id;
    }

    private void ApplyLocationSettings(Location location)
    {
        location.DefaultAssigneeUserId = DefaultAssigneeUserId;
        location.Active = Active;
    }

    private IActionResult RedirectToLocationsWithPreservedFilters(string slug)
    {
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}

