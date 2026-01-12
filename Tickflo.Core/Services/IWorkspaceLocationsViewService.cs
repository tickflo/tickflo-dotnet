using Tickflo.Core.Services;

namespace Tickflo.Core.Services;

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
