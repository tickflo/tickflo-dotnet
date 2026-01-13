using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceInventoryEditViewData
{
    public bool CanViewInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool CanCreateInventory { get; set; }
    public Inventory? ExistingItem { get; set; }
    public List<Location> LocationOptions { get; set; } = new();
}

public interface IWorkspaceInventoryEditViewService
{
    Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0);
}
