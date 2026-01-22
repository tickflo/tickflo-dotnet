namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;
using InventoryEntity = Entities.Inventory;

public class WorkspaceInventoryEditViewData
{
    public bool CanViewInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool CanCreateInventory { get; set; }
    public InventoryEntity? ExistingItem { get; set; }
    public List<Location> LocationOptions { get; set; } = [];
}

public interface IWorkspaceInventoryEditViewService
{
    public Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0);
}



