using Tickflo.Core.Services.Locations;

namespace Tickflo.Core.Services.Views;

public interface IWorkspaceLocationsViewService
{
    Task<WorkspaceLocationsViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceLocationsViewData
{
    public List<ILocationListingService.LocationItem> Locations { get; set; } = new();
    public bool CanCreateLocations { get; set; }
    public bool CanEditLocations { get; set; }
}



