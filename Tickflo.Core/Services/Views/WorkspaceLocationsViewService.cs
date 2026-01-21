namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Workspace;

public class WorkspaceLocationsViewService(
    IWorkspaceAccessService workspaceAccessService,
    ILocationListingService listingService) : IWorkspaceLocationsViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly ILocationListingService _listingService = listingService;

    public async Task<WorkspaceLocationsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceLocationsViewData();

        // Get permissions
        var permissions = await this._workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        if (permissions.TryGetValue("locations", out var locationPermissions))
        {
            data.CanCreateLocations = locationPermissions.CanCreate;
            data.CanEditLocations = locationPermissions.CanEdit;
        }

        // Load locations
        var locations = await this._listingService.GetListAsync(workspaceId);
        data.Locations = [.. locations];

        return data;
    }
}



