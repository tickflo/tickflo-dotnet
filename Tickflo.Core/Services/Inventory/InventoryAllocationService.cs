namespace Tickflo.Core.Services.Inventory;

using Tickflo.Core.Data;
using InventoryEntity = Entities.Inventory;

/// <summary>
/// Handles the business workflow of allocating inventory to locations and tracking inventory items.
/// </summary>
public class InventoryAllocationService(
    IInventoryRepository inventoryRepository,
    ILocationRepository locationRepository) : IInventoryAllocationService
{
    private readonly IInventoryRepository inventoryRepository = inventoryRepository;
    private readonly ILocationRepository locationRepository = locationRepository;

    /// <summary>
    /// Validates that a location exists and is active.
    /// </summary>
    private async Task ValidateLocationAsync(int workspaceId, int locationId)
    {
        var location = await this.locationRepository.FindAsync(workspaceId, locationId) ?? throw new InvalidOperationException("Location not found");

        if (!location.Active)
        {
            throw new InvalidOperationException("Cannot allocate inventory to inactive location");
        }
    }

    /// <summary>
    /// Registers a new inventory item in the system.
    /// </summary>
    public async Task<InventoryEntity> RegisterInventoryItemAsync(
        int workspaceId,
        InventoryRegistrationRequest request,
        int createdByUserId)
    {
        // Business rule: SKU must be unique and valid
        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new InvalidOperationException("SKU is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Item name is required");
        }

        var sku = request.Sku.Trim();

        // Enforce uniqueness
        var existingItems = await this.inventoryRepository.ListAsync(workspaceId);
        if (existingItems.Any(i => string.Equals(i.Sku, sku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"SKU '{sku}' already exists in this workspace");
        }

        // Business rule: Quantity cannot be negative
        if (request.InitialQuantity < 0)
        {
            throw new InvalidOperationException("Initial quantity cannot be negative");
        }

        // Validate location if specified
        if (request.LocationId.HasValue)
        {
            await this.ValidateLocationAsync(workspaceId, request.LocationId.Value);
        }

        var inventory = new InventoryEntity
        {
            WorkspaceId = workspaceId,
            Sku = sku,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Quantity = request.InitialQuantity,
            Cost = request.UnitCost ?? 0,
            LocationId = request.LocationId,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "active" : request.Status.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await this.inventoryRepository.CreateAsync(inventory);

        return inventory;
    }

    /// <summary>
    /// Allocates inventory to a specific location.
    /// </summary>
    public async Task<InventoryEntity> AllocateToLocationAsync(
        int workspaceId,
        int inventoryId,
        int locationId,
        int allocatedByUserId)
    {
        var inventory = await this.inventoryRepository.FindAsync(workspaceId, inventoryId) ?? throw new InvalidOperationException("Inventory item not found");

        // Validate location
        await this.ValidateLocationAsync(workspaceId, locationId);

        // Business rule: Track allocation changes
        var previousLocationId = inventory.LocationId;

        inventory.LocationId = locationId;
        inventory.UpdatedAt = DateTime.UtcNow;

        await this.inventoryRepository.UpdateAsync(inventory);

        // Could add: Log allocation change, notify location manager, etc.

        return inventory;
    }

    /// <summary>
    /// Updates inventory item details.
    /// Note: This method allows direct quantity updates for use cases like physical inventory counts
    /// or bulk editing. For tracked quantity changes with audit trails, use InventoryAdjustmentService instead.
    /// </summary>
    public async Task<InventoryEntity> UpdateInventoryDetailsAsync(
        int workspaceId,
        int inventoryId,
        InventoryDetailsUpdateRequest request,
        int updatedByUserId)
    {
        var inventory = await this.inventoryRepository.FindAsync(workspaceId, inventoryId) ?? throw new InvalidOperationException("Inventory item not found");

        // Update SKU if changed
        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var sku = request.Sku.Trim();

            if (!string.Equals(inventory.Sku, sku, StringComparison.OrdinalIgnoreCase))
            {
                // Check uniqueness
                var existingItems = await this.inventoryRepository.ListAsync(workspaceId);
                if (existingItems.Any(i => i.Id != inventoryId &&
                    string.Equals(i.Sku, sku, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"SKU '{sku}' already exists in this workspace");
                }

                inventory.Sku = sku;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            inventory.Name = request.Name.Trim();
        }

        if (request.Description != null)
        {
            inventory.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.UnitCost.HasValue)
        {
            inventory.Cost = request.UnitCost.Value;
        }

        if (request.Quantity.HasValue)
        {
            inventory.Quantity = request.Quantity.Value;
        }

        if (request.LocationId.HasValue)
        {
            var locationId = request.LocationId.Value;
            
            // Validate location if specified and not null (0 means clear location)
            if (locationId > 0)
            {
                await this.ValidateLocationAsync(workspaceId, locationId);
                inventory.LocationId = locationId;
            }
            else
            {
                inventory.LocationId = null;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            inventory.Status = request.Status.Trim();
        }

        inventory.UpdatedAt = DateTime.UtcNow;

        await this.inventoryRepository.UpdateAsync(inventory);

        return inventory;
    }

    /// <summary>
    /// Removes an inventory item from the system.
    /// </summary>
    public async Task RemoveInventoryItemAsync(int workspaceId, int inventoryId)
    {
        var inventory = await this.inventoryRepository.FindAsync(workspaceId, inventoryId) ?? throw new InvalidOperationException("Inventory item not found");

        // Business rule: Could check if item is referenced by tickets
        // Business rule: Could require zero quantity before deletion

        await this.inventoryRepository.DeleteAsync(workspaceId, inventoryId);
    }
}

/// <summary>
/// Request to register a new inventory item.
/// </summary>
public class InventoryRegistrationRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int InitialQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public int? LocationId { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Request to update inventory item details.
/// </summary>
public class InventoryDetailsUpdateRequest
{
    public string? Sku { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? UnitCost { get; set; }
    public int? Quantity { get; set; }
    public int? LocationId { get; set; }
    public string? Status { get; set; }
}
