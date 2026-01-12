using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceInventoryViewService : IWorkspaceInventoryViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IInventoryListingService _listingService;

    public WorkspaceInventoryViewService(
        IWorkspaceAccessService workspaceAccessService,
        IInventoryListingService listingService)
    {
        _workspaceAccessService = workspaceAccessService;
        _listingService = listingService;
    }

    public async Task<WorkspaceInventoryViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceInventoryViewData();

        // Check if user is admin
        data.IsWorkspaceAdmin = await _workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);

        // Get user's permissions
        var permissions = await _workspaceAccessService.GetUserPermissionsAsync(workspaceId, userId);
        
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
        data.Items = await _listingService.GetListAsync(workspaceId, null, null);

        return data;
    }
}
