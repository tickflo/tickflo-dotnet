namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class InventoryRepository(TickfloDbContext dbContext) : IInventoryRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<IEnumerable<Inventory>> ListAsync(int workspaceId, string? query = null, string? status = null)
    {
        var q = this.dbContext.Inventory.Where(i => i.WorkspaceId == workspaceId);
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

    public Task<Inventory?> FindAsync(int workspaceId, int id) => this.dbContext.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Id == id);

    public Task<Inventory?> FindBySkuAsync(int workspaceId, string sku) => this.dbContext.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Sku == sku);

    public async Task<Inventory> CreateAsync(Inventory item)
    {
        this.dbContext.Inventory.Add(item);
        await this.dbContext.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(Inventory item)
    {
        this.dbContext.Inventory.Update(item);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int workspaceId, int id)
    {
        var existing = await this.FindAsync(workspaceId, id);
        if (existing != null)
        {
            this.dbContext.Inventory.Remove(existing);
            await this.dbContext.SaveChangesAsync();
        }
    }
}
