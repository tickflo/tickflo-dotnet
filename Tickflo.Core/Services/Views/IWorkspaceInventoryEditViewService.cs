using Tickflo.Core.Entities;
using InventoryEntity = Tickflo.Core.Entities.Inventory;

namespace Tickflo.Core.Services.Views;

public class WorkspaceInventoryEditViewData
{
    public bool CanViewInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool CanCreateInventory { get; set; }
    public InventoryEntity? ExistingItem { get; set; }
    public List<Location> LocationOptions { get; set; } = new();
}

public interface IWorkspaceInventoryEditViewService
{
    Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0);
}



