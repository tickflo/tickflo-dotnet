namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IInventoryRepository
{
    public Task<IEnumerable<Inventory>> ListAsync(int workspaceId, string? query = null, string? status = null);
    public Task<Inventory?> FindAsync(int workspaceId, int id);
    public Task<Inventory?> FindBySkuAsync(int workspaceId, string sku);
    public Task<Inventory> CreateAsync(Inventory item);
    public Task UpdateAsync(Inventory item);
    public Task DeleteAsync(int workspaceId, int id);
}
