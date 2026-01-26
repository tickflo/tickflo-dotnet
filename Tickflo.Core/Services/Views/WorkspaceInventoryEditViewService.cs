namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
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


public class WorkspaceInventoryEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IInventoryRepository inventoryRepository,
    ILocationRepository locationRepository) : IWorkspaceInventoryEditViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IInventoryRepository inventoryRepository = inventoryRepository;
    private readonly ILocationRepository locationRepository = locationRepository;

    public async Task<WorkspaceInventoryEditViewData> BuildAsync(int workspaceId, int userId, int inventoryId = 0)
    {
        var data = new WorkspaceInventoryEditViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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

        var locations = await this.locationRepository.ListAsync(workspaceId);
        data.LocationOptions = locations != null ? [.. locations] : [];

        if (inventoryId > 0)
        {
            data.ExistingItem = await this.inventoryRepository.FindAsync(workspaceId, inventoryId);
        }
        else
        {
            data.ExistingItem = new InventoryEntity { WorkspaceId = workspaceId, Status = "active" };
        }

        return data;
    }
}



