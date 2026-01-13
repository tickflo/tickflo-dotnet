using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Inventory;

public class InventoryListingService : IInventoryListingService
{
    private readonly IInventoryRepository _inventoryRepo;

    public InventoryListingService(IInventoryRepository inventoryRepo)
    {
        _inventoryRepo = inventoryRepo;
    }

    public async Task<IReadOnlyList<Core.Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null)
    {
        // Repository already supports filtering, delegate directly
        var result = await _inventoryRepo.ListAsync(workspaceId, searchQuery, statusFilter);
        return result.ToList().AsReadOnly();
    }
}



