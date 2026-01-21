namespace Tickflo.Core.Services.Inventory;

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



