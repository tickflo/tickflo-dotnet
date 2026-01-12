using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public interface IWorkspaceInventoryViewService
{
    Task<WorkspaceInventoryViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceInventoryViewData
{
    public IEnumerable<Inventory> Items { get; set; } = new List<Inventory>();
    public bool CanCreateInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool IsWorkspaceAdmin { get; set; }
}
