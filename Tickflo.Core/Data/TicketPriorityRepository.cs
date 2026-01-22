namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TicketPriorityRepository(TickfloDbContext dbContext) : ITicketPriorityRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<IReadOnlyList<TicketPriority>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await this.dbContext.Set<TicketPriority>()
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<TicketPriority?> FindAsync(int workspaceId, string name, CancellationToken ct = default)
        => await this.dbContext.Set<TicketPriority>()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Name == name, ct);

    public async Task<TicketPriority> CreateAsync(TicketPriority priority, CancellationToken ct = default)
    {
        this.dbContext.Set<TicketPriority>().Add(priority);
        await this.dbContext.SaveChangesAsync(ct);
        return priority;
    }

    public async Task<TicketPriority> UpdateAsync(TicketPriority priority, CancellationToken ct = default)
    {
        this.dbContext.Set<TicketPriority>().Update(priority);
        await this.dbContext.SaveChangesAsync(ct);
        return priority;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await this.dbContext.Set<TicketPriority>().FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == id, ct);
        if (entity == null)
        {
            return false;
        }

        this.dbContext.Set<TicketPriority>().Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);
        return true;
    }
}
