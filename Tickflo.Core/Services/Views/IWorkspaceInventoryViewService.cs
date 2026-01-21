namespace Tickflo.Core.Services.Views;

using InventoryEntity = Entities.Inventory;

public interface IWorkspaceInventoryViewService
{
    public Task<WorkspaceInventoryViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceInventoryViewData
{
    public IEnumerable<InventoryEntity> Items { get; set; } = [];
    public bool CanCreateInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool IsWorkspaceAdmin { get; set; }
}


