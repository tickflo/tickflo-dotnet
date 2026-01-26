namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Services.Workspace;
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


public class WorkspaceInventoryViewService(
    IWorkspaceAccessService workspaceAccessService,
    IInventoryListingService contactListingService) : IWorkspaceInventoryViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IInventoryListingService contactListingService = contactListingService;

    public async Task<WorkspaceInventoryViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceInventoryViewData
        {
            // Check if user is admin
            IsWorkspaceAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId)
        };

        // Get user's permissions
        var permissions = await this.workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

        if (permissions.TryGetValue("inventory", out var inventoryPermissions))
        {
            data.CanCreateInventory = inventoryPermissions.CanCreate || data.IsWorkspaceAdmin;
            data.CanEditInventory = inventoryPermissions.CanEdit || data.IsWorkspaceAdmin;
        }
        else
        {
            data.CanCreateInventory = data.IsWorkspaceAdmin;
            data.CanEditInventory = data.IsWorkspaceAdmin;
        }

        // Load inventory items (all items, filtering is applied in the page)
        data.Items = await this.contactListingService.GetListAsync(workspaceId, null, null);

        return data;
    }
}



