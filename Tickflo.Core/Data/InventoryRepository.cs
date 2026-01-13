using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly TickfloDbContext _db;
        public InventoryRepository(TickfloDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Inventory>> ListAsync(int workspaceId, string? query = null, string? status = null)
        {
            var q = _db.Inventory.Where(i => i.WorkspaceId == workspaceId);
            if (!string.IsNullOrWhiteSpace(query))
            {
                q = q.Where(i => i.Name.Contains(query) || i.Sku.Contains(query));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(i => i.Status == status);
            }
            return await q.OrderBy(i => i.Name).ToListAsync();
        }

        public Task<Inventory?> FindAsync(int workspaceId, int id)
        {
            return _db.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Id == id);
        }

        public Task<Inventory?> FindBySkuAsync(int workspaceId, string sku)
        {
            return _db.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Sku == sku);
        }

        public async Task<Inventory> CreateAsync(Inventory item)
        {
            _db.Inventory.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(Inventory item)
        {
            _db.Inventory.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int workspaceId, int id)
        {
            var existing = await FindAsync(workspaceId, id);
            if (existing != null)
            {
                _db.Inventory.Remove(existing);
                await _db.SaveChangesAsync();
            }
        }
    }
}
