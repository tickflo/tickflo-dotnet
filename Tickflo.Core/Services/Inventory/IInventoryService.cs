using Tickflo.Core.Entities;
using InventoryEntity = Tickflo.Core.Entities.Inventory;

namespace Tickflo.Core.Services.Inventory;

/// <summary>
/// Service for managing inventory items.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Creates a new inventory item.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Inventory creation details</param>
    /// <returns>Created inventory item</returns>
    Task<InventoryEntity> CreateInventoryAsync(int workspaceId, CreateInventoryRequest request);

    /// <summary>
    /// Updates an existing inventory item.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item to update</param>
    /// <param name="request">Update details</param>
    /// <returns>Updated inventory item</returns>
    Task<InventoryEntity> UpdateInventoryAsync(int workspaceId, int inventoryId, UpdateInventoryRequest request);

    /// <summary>
    /// Deletes an inventory item.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="inventoryId">Inventory item to delete</param>
    Task DeleteInventoryAsync(int workspaceId, int inventoryId);

    /// <summary>
    /// Validates SKU uniqueness within a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="sku">SKU to check</param>
    /// <param name="excludeInventoryId">Optional inventory ID to exclude</param>
    /// <returns>True if SKU is unique</returns>
    Task<bool> IsSkuUniqueAsync(int workspaceId, string sku, int? excludeInventoryId = null);
}

public class CreateInventoryRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? LocationId { get; set; }
}

public class UpdateInventoryRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? LocationId { get; set; }
}




