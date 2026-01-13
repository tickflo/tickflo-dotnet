using Tickflo.Core.Entities;
using InventoryEntity = Tickflo.Core.Entities.Inventory;

namespace Tickflo.Core.Services.Views;

public interface IWorkspaceInventoryViewService
{
    Task<WorkspaceInventoryViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceInventoryViewData
{
    public IEnumerable<InventoryEntity> Items { get; set; } = new List<InventoryEntity>();
    public bool CanCreateInventory { get; set; }
    public bool CanEditInventory { get; set; }
    public bool IsWorkspaceAdmin { get; set; }
}


