namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Services.Workspace;

public class WorkspaceInventoryViewService(
    IWorkspaceAccessService workspaceAccessService,
    IInventoryListingService listingService) : IWorkspaceInventoryViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly IInventoryListingService _listingService = listingService;

    public async Task<WorkspaceInventoryViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceInventoryViewData
        {
            // Check if user is admin
            IsWorkspaceAdmin = await this._workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId)
        };

        // Get user's permissions
        var permissions = await this._workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);

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
        data.Items = await this._listingService.GetListAsync(workspaceId, null, null);

        return data;
    }
}



