namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class InventoryRepository(TickfloDbContext db) : IInventoryRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task<IEnumerable<Inventory>> ListAsync(int workspaceId, string? query = null, string? status = null)
    {
        var q = this._db.Inventory.Where(i => i.WorkspaceId == workspaceId);
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

    public Task<Inventory?> FindAsync(int workspaceId, int id) => this._db.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Id == id);

    public Task<Inventory?> FindBySkuAsync(int workspaceId, string sku) => this._db.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Sku == sku);

    public async Task<Inventory> CreateAsync(Inventory item)
    {
        this._db.Inventory.Add(item);
        await this._db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(Inventory item)
    {
        this._db.Inventory.Update(item);
        await this._db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int workspaceId, int id)
    {
        var existing = await this.FindAsync(workspaceId, id);
        if (existing != null)
        {
            this._db.Inventory.Remove(existing);
            await this._db.SaveChangesAsync();
        }
    }
}
