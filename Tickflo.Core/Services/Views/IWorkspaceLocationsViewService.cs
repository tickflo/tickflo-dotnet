namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Locations;

public interface IWorkspaceLocationsViewService
{
    public Task<WorkspaceLocationsViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceLocationsViewData
{
    public List<ILocationListingService.LocationItem> Locations { get; set; } = [];
    public bool CanCreateLocations { get; set; }
    public bool CanEditLocations { get; set; }
}



