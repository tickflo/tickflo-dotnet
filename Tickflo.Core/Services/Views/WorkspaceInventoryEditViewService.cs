namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using InventoryEntity = Entities.Inventory;

public class WorkspaceInventoryEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    IInventoryRepository inventoryRepo,
    ILocationRepository locationRepo) : IWorkspaceInventoryEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly IInventoryRepository _inventoryRepo = inventoryRepo;
    private readonly ILocationRepository _locationRepo = locationRepo;

    public async Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0)
    {
        var data = new WorkspaceInventoryEditViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewInventory = data.CanEditInventory = data.CanCreateInventory = true;
        }
        else if (eff.TryGetValue("inventory", out var ip))
        {
            data.CanViewInventory = ip.CanView;
            data.CanEditInventory = ip.CanEdit;
            data.CanCreateInventory = ip.CanCreate;
        }

        var locations = await this._locationRepo.ListAsync(workspaceId);
        data.LocationOptions = locations != null ? [.. locations] : [];

        if (inventoryId > 0)
        {
            data.ExistingItem = await this._inventoryRepo.FindAsync(workspaceId, inventoryId);
        }
        else
        {
            data.ExistingItem = new InventoryEntity { WorkspaceId = workspaceId, Status = "active" };
        }

        return data;
    }
}



