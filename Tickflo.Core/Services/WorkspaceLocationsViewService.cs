using Tickflo.Core.Data;
using Tickflo.Core.Services;

namespace Tickflo.Core.Services;

public class WorkspaceLocationsViewService : IWorkspaceLocationsViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly ILocationListingService _listingService;

    public WorkspaceLocationsViewService(
        IWorkspaceAccessService workspaceAccessService,
        ILocationListingService listingService)
    {
        _workspaceAccessService = workspaceAccessService;
        _listingService = listingService;
    }

    public async Task<WorkspaceLocationsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceLocationsViewData();

        // Get permissions
        var permissions = await _workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        if (permissions.TryGetValue("locations", out var locationPermissions))
        {
            data.CanCreateLocations = locationPermissions.CanCreate;
            data.CanEditLocations = locationPermissions.CanEdit;
        }

        // Load locations
        var locations = await _listingService.GetListAsync(workspaceId);
        data.Locations = locations.ToList();

        return data;
    }
}
