namespace Tickflo.Core.Services.Inventory;

using Tickflo.Core.Data;
using InventoryEntity = Entities.Inventory;

/// <summary>
/// Handles inventory quantity adjustments, tracking stock changes.
/// </summary>
public class InventoryAdjustmentService(IInventoryRepository inventoryRepository) : IInventoryAdjustmentService
{
    private readonly IInventoryRepository inventoryRepository = inventoryRepository;

    /// <summary>
    /// Increases inventory quantity (e.g., receiving stock, returns).
    /// </summary>
    public async Task<InventoryEntity> IncreaseQuantityAsync(
        int workspaceId,
        int inventoryId,
        int amount,
        string reason,
        int adjustedByUserId)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Increase amount must be positive");
        }

        var inventory = await this.inventoryRepository.FindAsync(workspaceId, inventoryId) ?? throw new InvalidOperationException("Inventory item not found");

        // Business rule: Check for overflow
        if (inventory.Quantity + amount < 0)
        {
            throw new InvalidOperationException("Quantity overflow detected");
        }

        inventory.Quantity += amount;
        inventory.UpdatedAt = DateTime.UtcNow;

        await this.inventoryRepository.UpdateAsync(inventory);

        // Could add: Log adjustment history, trigger reorder alerts, etc.

        return inventory;
    }

    /// <summary>
    /// Decreases inventory quantity (e.g., usage, allocation to ticket).
    /// </summary>
    public async Task<InventoryEntity> DecreaseQuantityAsync(
        int workspaceId,
        int inventoryId,
        int amount,
        string reason,
        int adjustedByUserId)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Decrease amount must be positive");
        }

        var inventory = await this.inventoryRepository.FindAsync(workspaceId, inventoryId) ?? throw new InvalidOperationException("Inventory item not found");

        // Business rule: Prevent negative inventory
        if (inventory.Quantity - amount < 0)
        {
            throw new InvalidOperationException($"Insufficient inventory. Available: {inventory.Quantity}, Requested: {amount}");
        }

        inventory.Quantity -= amount;
        inventory.UpdatedAt = DateTime.UtcNow;

        await this.inventoryRepository.UpdateAsync(inventory);

        // Could add: Log adjustment, notify if below reorder point, etc.

        return inventory;
    }

    /// <summary>
    /// Sets inventory to a specific quantity (e.g., after physical count).
    /// </summary>
    public async Task<InventoryEntity> SetQuantityAsync(
        int workspaceId,
        int inventoryId,
        int newQuantity,
        string reason,
        int adjustedByUserId)
    {
        if (newQuantity < 0)
        {
            throw new InvalidOperationException("Quantity cannot be negative");
        }

        var inventory = await this.inventoryRepository.FindAsync(workspaceId, inventoryId) ?? throw new InvalidOperationException("Inventory item not found");

        var previousQuantity = inventory.Quantity;
        var variance = newQuantity - previousQuantity;

        inventory.Quantity = newQuantity;
        inventory.UpdatedAt = DateTime.UtcNow;

        await this.inventoryRepository.UpdateAsync(inventory);

        // Could add: Log variance for audit, investigate large discrepancies

        return inventory;
    }
}
