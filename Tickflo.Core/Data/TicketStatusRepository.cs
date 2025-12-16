using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TicketStatusRepository(TickfloDbContext db) : ITicketStatusRepository
{
    public async Task<IReadOnlyList<TicketStatus>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await db.TicketStatuses.Where(s => s.WorkspaceId == workspaceId).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);

    public async Task<TicketStatus?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default)
        => await db.TicketStatuses.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == id, ct);

    public async Task<TicketStatus?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default)
        => await db.TicketStatuses.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Name == name, ct);

    public async Task<TicketStatus> CreateAsync(TicketStatus status, CancellationToken ct = default)
    {
        db.TicketStatuses.Add(status);
        await db.SaveChangesAsync(ct);
        return status;
    }

    public async Task<TicketStatus> UpdateAsync(TicketStatus status, CancellationToken ct = default)
    {
        db.TicketStatuses.Update(status);
        await db.SaveChangesAsync(ct);
        return status;
    }

    public async Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(workspaceId, id, ct);
        if (entity != null)
        {
            db.TicketStatuses.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }
}
