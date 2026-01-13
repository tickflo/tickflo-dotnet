using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceInventoryEditViewService : IWorkspaceInventoryEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ILocationRepository _locationRepo;

    public WorkspaceInventoryEditViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IInventoryRepository inventoryRepo,
        ILocationRepository locationRepo)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _inventoryRepo = inventoryRepo;
        _locationRepo = locationRepo;
    }

    public async Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0)
    {
        var data = new WorkspaceInventoryEditViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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

        var locations = await _locationRepo.ListAsync(workspaceId);
        data.LocationOptions = locations != null ? locations.ToList() : new();

        if (inventoryId > 0)
        {
            data.ExistingItem = await _inventoryRepo.FindAsync(workspaceId, inventoryId);
        }
        else
        {
            data.ExistingItem = new Inventory { WorkspaceId = workspaceId, Status = "active" };
        }

        return data;
    }
}
