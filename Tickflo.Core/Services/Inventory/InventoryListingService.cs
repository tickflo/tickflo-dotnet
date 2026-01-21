namespace Tickflo.Core.Services.Inventory;

using Tickflo.Core.Data;

public class InventoryListingService(IInventoryRepository inventoryRepo) : IInventoryListingService
{
    private readonly IInventoryRepository _inventoryRepo = inventoryRepo;

    public async Task<IReadOnlyList<Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null)
    {
        // Repository already supports filtering, delegate directly
        var result = await this._inventoryRepo.ListAsync(workspaceId, searchQuery, statusFilter);
        return result.ToList().AsReadOnly();
    }
}



