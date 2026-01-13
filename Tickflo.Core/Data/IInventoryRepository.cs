using System.Collections.Generic;
using System.Threading.Tasks;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data
{
    public interface IInventoryRepository
    {
        Task<IEnumerable<Inventory>> ListAsync(int workspaceId, string? query = null, string? status = null);
        Task<Inventory?> FindAsync(int workspaceId, int id);
        Task<Inventory?> FindBySkuAsync(int workspaceId, string sku);
        Task<Inventory> CreateAsync(Inventory item);
        Task UpdateAsync(Inventory item);
        Task DeleteAsync(int workspaceId, int id);
    }
}
