namespace Tickflo.Core.Services;

public interface IInventoryListingService
{
    /// <summary>
    /// Gets inventory items for a workspace with optional filtering.
    /// </summary>
    Task<IReadOnlyList<Core.Entities.Inventory>> GetListAsync(
        int workspaceId,
        string? searchQuery = null,
        string? statusFilter = null);
}
