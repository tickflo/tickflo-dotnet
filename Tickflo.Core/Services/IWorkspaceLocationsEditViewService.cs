using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceLocationsEditViewData
{
    public bool CanViewLocations { get; set; }
    public bool CanEditLocations { get; set; }
    public bool CanCreateLocations { get; set; }
    public Location? ExistingLocation { get; set; }
    public List<int> SelectedContactIds { get; set; } = new();
    public List<User> MemberOptions { get; set; } = new();
    public List<Contact> ContactOptions { get; set; } = new();
}

public interface IWorkspaceLocationsEditViewService
{
    Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0);
}
