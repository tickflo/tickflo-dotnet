namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceLocationsEditViewData
{
    public bool CanViewLocations { get; set; }
    public bool CanEditLocations { get; set; }
    public bool CanCreateLocations { get; set; }
    public Location? ExistingLocation { get; set; }
    public List<int> SelectedContactIds { get; set; } = [];
    public List<User> MemberOptions { get; set; } = [];
    public List<Contact> ContactOptions { get; set; } = [];
}

public interface IWorkspaceLocationsEditViewService
{
    public Task<WorkspaceLocationsEditViewData> BuildAsync(int workspaceId, int userId, int locationId = 0);
}


