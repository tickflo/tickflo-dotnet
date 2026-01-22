namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Workspace;

public class WorkspaceLocationsViewService(
    IWorkspaceAccessService workspaceAccessService,
    ILocationListingService contactListingService) : IWorkspaceLocationsViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly ILocationListingService contactListingService = contactListingService;

    public async Task<WorkspaceLocationsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceLocationsViewData();

        // Get permissions
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        if (permissions.TryGetValue("locations", out var locationPermissions))
        {
            data.CanCreateLocations = locationPermissions.CanCreate;
            data.CanEditLocations = locationPermissions.CanEdit;
        }

        // Load locations
        var locations = await this.contactListingService.GetListAsync(workspaceId);
        data.Locations = [.. locations];

        return data;
    }
}



