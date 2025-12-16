using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TicketPriorityRepository(TickfloDbContext db) : ITicketPriorityRepository
{
    public async Task<IReadOnlyList<TicketPriority>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await db.Set<TicketPriority>()
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<TicketPriority?> FindAsync(int workspaceId, string name, CancellationToken ct = default)
        => await db.Set<TicketPriority>()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Name == name, ct);

    public async Task<TicketPriority> CreateAsync(TicketPriority priority, CancellationToken ct = default)
    {
        db.Set<TicketPriority>().Add(priority);
        await db.SaveChangesAsync(ct);
        return priority;
    }

    public async Task<TicketPriority> UpdateAsync(TicketPriority priority, CancellationToken ct = default)
    {
        db.Set<TicketPriority>().Update(priority);
        await db.SaveChangesAsync(ct);
        return priority;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await db.Set<TicketPriority>().FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == id, ct);
        if (entity == null) return false;
        db.Set<TicketPriority>().Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
