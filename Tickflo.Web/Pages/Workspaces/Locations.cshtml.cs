namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class LocationsModel(
    IWorkspaceRepository workspaceRepo,
    IUserWorkspaceRepository userWorkspaceRepository,
    ILocationRepository locationRepository,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceLocationsViewService viewService) : WorkspacePageModel
{
    #region Constants
    private const string LocationDeletedFormat = "Location #{0} deleted.";
    private const string LocationNotFoundMessage = "Location not found.";
    private const string LocationsSection = "locations";
    private const string EditAction = "edit";
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ILocationRepository locationRepository = locationRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly IWorkspaceLocationsViewService _viewService = viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ILocationListingService.LocationItem> Locations { get; private set; } = [];
    public bool CanCreateLocations { get; private set; }
    public bool CanEditLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var result = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (result is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, uid);
        this.Locations = viewData.Locations;
        this.CanCreateLocations = viewData.CanCreateLocations;
        this.CanEditLocations = viewData.CanEditLocations;

        return this.Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int locationId)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(this.Workspace) is IActionResult result)
        {
            return result;
        }

        if (!this._currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Forbid();
        }

        if (!await this.CanUserEditLocationsAsync(uid))
        {
            return this.Forbid();
        }

        var ok = await this.locationRepository.DeleteAsync(this.Workspace!.Id, locationId);
        this.SetSuccessMessage(ok
            ? string.Format(LocationDeletedFormat, locationId)
            : LocationNotFoundMessage);

        return this.RedirectToPage("/Workspaces/Locations", new { slug });
    }

    private async Task<bool> CanUserEditLocationsAsync(int userId) => await this._workspaceAccessService.CanUserPerformActionAsync(
            this.Workspace!.Id, userId, LocationsSection, EditAction);
}


