using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;
using Tickflo.Core.Services.Locations;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class LocationsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IWorkspaceLocationsViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ILocationListingService.LocationItem> Locations { get; private set; } = new();
    public bool CanCreateLocations { get; private set; }
    public bool CanEditLocations { get; private set; }

    public LocationsModel(
        IWorkspaceRepository workspaceRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ILocationRepository locationRepo,
        ICurrentUserService currentUserService,
        IWorkspaceAccessService workspaceAccessService,
        IWorkspaceLocationsViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _locationRepo = locationRepo;
        _currentUserService = currentUserService;
        _workspaceAccessService = workspaceAccessService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace!.Id, uid);
        Locations = viewData.Locations;
        CanCreateLocations = viewData.CanCreateLocations;
        CanEditLocations = viewData.CanEditLocations;

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string slug, int locationId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;

        if (!_currentUserService.TryGetUserId(User, out var uid)) return Forbid();

        var canDelete = await _workspaceAccessService.CanUserPerformActionAsync(
            Workspace!.Id, uid, "locations", "edit");
        if (!canDelete) return Forbid();

        var ok = await _locationRepo.DeleteAsync(Workspace.Id, locationId);
        SetSuccessMessage(ok ? $"Location #{locationId} deleted." : "Location not found.");
        return RedirectToPage("/Workspaces/Locations", new { slug });
    }
}


