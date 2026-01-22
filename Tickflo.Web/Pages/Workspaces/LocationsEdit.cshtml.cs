namespace Tickflo.Web.Pages.Workspaces;

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Views;

[Authorize]
public class LocationsEditModel(
    IWorkspaceRepository workspaceRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    ILocationRepository locationRepository,
    IWorkspaceLocationsEditViewService workspaceLocationsEditViewService,
    ILocationSetupService locationSetupService) : WorkspacePageModel
{
    #region Constants
    private const int NewLocationId = 0;
    private static readonly CompositeFormat LocationCreatedSuccessfully = CompositeFormat.Parse("Location '{0}' created successfully.");
    private static readonly CompositeFormat LocationUpdatedSuccessfully = CompositeFormat.Parse("Location '{0}' updated successfully.");
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ILocationRepository locationRepository = locationRepository;
    private readonly IWorkspaceLocationsEditViewService workspaceLocationsEditViewService = workspaceLocationsEditViewService;
    private readonly ILocationSetupService locationSetupService = locationSetupService;
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
    public List<User> MemberOptions { get; private set; } = [];
    [BindProperty]
    public List<int> SelectedContactIds { get; set; } = [];
    public List<Contact> ContactOptions { get; private set; } = [];
    public bool CanViewLocations { get; private set; }
    public bool CanEditLocations { get; private set; }
    public bool CanCreateLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int locationId = 0)
    {
        this.WorkspaceSlug = slug;

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await this.workspaceLocationsEditViewService.BuildAsync(workspaceId, uid, locationId);
        this.CanViewLocations = viewData.CanViewLocations;
        this.CanEditLocations = viewData.CanEditLocations;
        this.CanCreateLocations = viewData.CanCreateLocations;

        if (this.EnsurePermissionOrForbid(this.CanViewLocations) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.MemberOptions = viewData.MemberOptions;
        this.ContactOptions = viewData.ContactOptions;

        if (locationId > NewLocationId)
        {
            this.LoadExistingLocationData(viewData);
        }
        else
        {
            this.InitializeNewLocationForm();
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await this.workspaceLocationsEditViewService.BuildAsync(workspaceId, uid, this.LocationId);
        if (this.EnsureCreateOrEditPermission(this.LocationId, viewData.CanCreateLocations, viewData.CanEditLocations) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var effectiveLocationId = this.LocationId;
        try
        {
            if (this.LocationId == NewLocationId)
            {
                effectiveLocationId = await this.CreateAndSaveLocationAsync(workspaceId, uid);
            }
            else
            {
                effectiveLocationId = await this.UpdateAndSaveLocationAsync(workspaceId, uid);
            }
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
            return this.Page();
        }

        await this.locationRepository.SetContactsAsync(workspaceId, effectiveLocationId, this.SelectedContactIds ?? []);
        return this.RedirectToLocationsWithPreservedFilters(slug);
    }

    private void LoadExistingLocationData(WorkspaceLocationsEditViewData viewData)
    {
        if (viewData.ExistingLocation == null)
        {
            return;
        }

        this.LocationId = viewData.ExistingLocation.Id;
        this.Name = viewData.ExistingLocation.Name ?? string.Empty;
        this.Address = viewData.ExistingLocation.Address ?? string.Empty;
        this.Active = viewData.ExistingLocation.Active;
        this.DefaultAssigneeUserId = viewData.ExistingLocation.DefaultAssigneeUserId;
        this.SelectedContactIds = viewData.SelectedContactIds;
    }

    private void InitializeNewLocationForm()
    {
        this.LocationId = NewLocationId;
        this.Name = string.Empty;
        this.Address = string.Empty;
        this.Active = true;
        this.DefaultAssigneeUserId = null;
        this.SelectedContactIds = [];
    }

    private async Task<int> CreateAndSaveLocationAsync(int workspaceId, int userId)
    {
        var created = await this.locationSetupService.CreateLocationAsync(workspaceId, new LocationCreationRequest
        {
            Name = this.Name?.Trim() ?? string.Empty,
            Address = this.Address?.Trim() ?? string.Empty
        }, userId);

        this.ApplyLocationSettings(created);
        await this.locationRepository.UpdateAsync(created);
        this.SetSuccessMessage(string.Format(null, LocationCreatedSuccessfully, created.Name));

        return created.Id;
    }

    private async Task<int> UpdateAndSaveLocationAsync(int workspaceId, int userId)
    {
        var updated = await this.locationSetupService.UpdateLocationDetailsAsync(workspaceId, this.LocationId, new LocationUpdateRequest
        {
            Name = this.Name?.Trim() ?? string.Empty,
            Address = this.Address?.Trim() ?? string.Empty
        }, userId);

        this.ApplyLocationSettings(updated);
        await this.locationRepository.UpdateAsync(updated);
        this.SetSuccessMessage(string.Format(null, LocationUpdatedSuccessfully, updated.Name));

        return updated.Id;
    }

    private void ApplyLocationSettings(Location location)
    {
        location.DefaultAssigneeUserId = this.DefaultAssigneeUserId;
        location.Active = this.Active;
    }

    private RedirectToPageResult RedirectToLocationsWithPreservedFilters(string slug)
    {
        var queryQ = this.Request.Query["Query"].ToString();
        var pageQ = this.Request.Query["PageNumber"].ToString();
        return this.RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}

