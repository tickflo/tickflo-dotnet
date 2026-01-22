namespace Tickflo.Core.Services.Inventory;

using Tickflo.Core.Data;

public class InventoryListingService(IInventoryRepository inventoryRepository) : IInventoryListingService
{
    private readonly IInventoryRepository inventoryRepository = inventoryRepository;

    public async Task<IReadOnlyList<Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null)
    {
        // Repository already supports filtering, delegate directly
        var result = await this.inventoryRepository.ListAsync(workspaceId, searchQuery, statusFilter);
        return result.ToList().AsReadOnly();
    }
}



