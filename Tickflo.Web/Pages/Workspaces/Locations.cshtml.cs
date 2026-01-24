namespace Tickflo.Web.Pages.Workspaces;

using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class LocationsModel(
    IWorkspaceService workspaceService,
    ILocationSetupService locationSetupService,
    ICurrentUserService currentUserService,
    IWorkspaceAccessService workspaceAccessService,
    IWorkspaceLocationsViewService workspaceLocationsViewService) : WorkspacePageModel
{
    #region Constants
    private static readonly CompositeFormat LocationDeletedFormat = CompositeFormat.Parse("Location #{0} deleted.");
    private const string LocationNotFoundMessage = "Location not found.";
    private const string LocationsSection = "locations";
    private const string EditAction = "edit";
    #endregion

    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly ILocationSetupService locationSetupService = locationSetupService;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IWorkspaceLocationsViewService workspaceLocationsViewService = workspaceLocationsViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ILocationListingService.LocationItem> Locations { get; private set; } = [];
    public bool CanCreateLocations { get; private set; }
    public bool CanEditLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceLocationsViewService.BuildAsync(this.Workspace.Id, uid);
        this.Locations = viewData.Locations;
        this.CanCreateLocations = viewData.CanCreateLocations;
        this.CanEditLocations = viewData.CanEditLocations;

        return this.Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int locationId)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(this.Workspace) is IActionResult result)
        {
            return result;
        }

        if (!this.currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Forbid();
        }

        if (!await this.CanUserEditLocationsAsync(uid))
        {
            return this.Forbid();
        }

        try
        {
            await this.locationSetupService.RemoveLocationAsync(this.Workspace!.Id, locationId);
            this.SetSuccessMessage(string.Format(null, LocationDeletedFormat, locationId));
        }
        catch (InvalidOperationException)
        {
            this.SetSuccessMessage(LocationNotFoundMessage);
        }

        return this.RedirectToPage("/Workspaces/Locations", new { slug });
    }

    private async Task<bool> CanUserEditLocationsAsync(int userId) => await this.workspaceAccessService.CanUserPerformActionAsync(
            this.Workspace!.Id, userId, LocationsSection, EditAction);
}


