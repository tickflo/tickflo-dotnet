namespace Tickflo.Core.Services.Inventory;

using InventoryEntity = Entities.Inventory;

/// <summary>
/// Handles inventory allocation and registration workflows.
/// </summary>
public interface IInventoryAllocationService
{
    /// <summary>
    /// Registers a new inventory item in the system.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Registration details</param>
    /// <param name="createdByUserId">User registering the item</param>
    /// <returns>The registered inventory item</returns>
    public Task<InventoryEntity> RegisterInventoryItemAsync(int workspaceId, InventoryRegistrationRequest request, int createdByUserId);

    /// <summary>
    /// Allocates an inventory item to a specific location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item to allocate</param>
    /// <param name="locationId">Target location</param>
    /// <param name="allocatedByUserId">User performing the allocation</param>
    /// <returns>The updated inventory item</returns>
    public Task<InventoryEntity> AllocateToLocationAsync(int workspaceId, int inventoryId, int locationId, int allocatedByUserId);

    /// <summary>
    /// Updates inventory item details (excluding quantity).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item to update</param>
    /// <param name="request">Update details</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated inventory item</returns>
    public Task<InventoryEntity> UpdateInventoryDetailsAsync(int workspaceId, int inventoryId, InventoryDetailsUpdateRequest request, int updatedByUserId);

    /// <summary>
    /// Removes an inventory item from the system.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item to remove</param>
    public Task RemoveInventoryItemAsync(int workspaceId, int inventoryId);
}
