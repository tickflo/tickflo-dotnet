using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using InventoryEntity = Tickflo.Core.Entities.Inventory;

namespace Tickflo.Core.Services.Inventory;

/// <summary>
/// Service for managing inventory items.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ILocationRepository _locationRepo;

    public InventoryService(
        IInventoryRepository inventoryRepo,
        ILocationRepository locationRepo)
    {
        _inventoryRepo = inventoryRepo;
        _locationRepo = locationRepo;
    }

    public async Task<InventoryEntity> CreateInventoryAsync(int workspaceId, CreateInventoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new InvalidOperationException("SKU is required");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        var sku = request.Sku.Trim();
        var name = request.Name.Trim();

        if (!await IsSkuUniqueAsync(workspaceId, sku))
            throw new InvalidOperationException($"SKU '{sku}' already exists");

        // Validate location if provided
        if (request.LocationId.HasValue)
        {
            var location = await _locationRepo.FindAsync(workspaceId, request.LocationId.Value);
            if (location == null)
                throw new InvalidOperationException("Location not found");
        }

        var inventory = new InventoryEntity
        {
            WorkspaceId = workspaceId,
            Sku = sku,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Quantity = request.Quantity,
            Cost = request.UnitPrice,
            LocationId = request.LocationId,
            CreatedAt = DateTime.UtcNow
        };

        await _inventoryRepo.CreateAsync(inventory);

        return inventory;
    }

    public async Task<InventoryEntity> UpdateInventoryAsync(int workspaceId, int inventoryId, UpdateInventoryRequest request)
    {
        var inventory = await _inventoryRepo.FindAsync(workspaceId, inventoryId);
        if (inventory == null)
            throw new InvalidOperationException("Inventory item not found");

        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new InvalidOperationException("SKU is required");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required");

        var sku = request.Sku.Trim();

        if (sku != inventory.Sku && !await IsSkuUniqueAsync(workspaceId, sku, inventoryId))
            throw new InvalidOperationException($"SKU '{sku}' already exists");

        // Validate location if provided
        if (request.LocationId.HasValue)
        {
            var location = await _locationRepo.FindAsync(workspaceId, request.LocationId.Value);
            if (location == null)
                throw new InvalidOperationException("Location not found");
        }

        inventory.Sku = sku;
        inventory.Name = request.Name.Trim();
        inventory.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        inventory.Quantity = request.Quantity;
        inventory.Cost = request.UnitPrice;
        inventory.LocationId = request.LocationId;
        inventory.UpdatedAt = DateTime.UtcNow;

        await _inventoryRepo.UpdateAsync(inventory);

        return inventory;
    }

    public async Task DeleteInventoryAsync(int workspaceId, int inventoryId)
    {
        await _inventoryRepo.DeleteAsync(workspaceId, inventoryId);
    }

    public async Task<bool> IsSkuUniqueAsync(int workspaceId, string sku, int? excludeInventoryId = null)
    {
        var items = await _inventoryRepo.ListAsync(workspaceId);
        var existing = items.FirstOrDefault(i => 
            string.Equals(i.Sku, sku, StringComparison.OrdinalIgnoreCase));
        
        return existing == null || (excludeInventoryId.HasValue && existing.Id == excludeInventoryId.Value);
    }
}





