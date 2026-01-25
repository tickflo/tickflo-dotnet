namespace Tickflo.Core.Services.Inventory;

using Tickflo.Core.Data;
public interface IInventoryListingService
{
    /// <summary>
    /// Gets inventory items for a workspace with optional filtering.
    /// </summary>
    public Task<IReadOnlyList<Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null);
}


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



