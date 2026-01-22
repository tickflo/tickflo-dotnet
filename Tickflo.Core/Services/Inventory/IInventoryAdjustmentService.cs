namespace Tickflo.Core.Services.Inventory;

using InventoryEntity = Entities.Inventory;

/// <summary>
/// Handles inventory quantity adjustments.
/// </summary>
public interface IInventoryAdjustmentService
{
    /// <summary>
    /// Increases inventory quantity.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item</param>
    /// <param name="amount">Amount to increase</param>
    /// <param name="reason">Reason for adjustment</param>
    /// <param name="adjustedByUserId">User performing adjustment</param>
    /// <returns>Updated inventory item</returns>
    public Task<InventoryEntity> IncreaseQuantityAsync(int workspaceId, int inventoryId, int amount, string reason, int adjustedByUserId);

    /// <summary>
    /// Decreases inventory quantity with validation.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item</param>
    /// <param name="amount">Amount to decrease</param>
    /// <param name="reason">Reason for adjustment</param>
    /// <param name="adjustedByUserId">User performing adjustment</param>
    /// <returns>Updated inventory item</returns>
    public Task<InventoryEntity> DecreaseQuantityAsync(int workspaceId, int inventoryId, int amount, string reason, int adjustedByUserId);

    /// <summary>
    /// Sets inventory to a specific quantity (e.g., physical count).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item</param>
    /// <param name="newQuantity">New quantity value</param>
    /// <param name="reason">Reason for adjustment</param>
    /// <param name="adjustedByUserId">User performing adjustment</param>
    /// <returns>Updated inventory item</returns>
    public Task<InventoryEntity> SetQuantityAsync(int workspaceId, int inventoryId, int newQuantity, string reason, int adjustedByUserId);
}
