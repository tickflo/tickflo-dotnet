namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TicketHistoryRepository(TickfloDbContext db) : ITicketHistoryRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task CreateAsync(TicketHistory history)
    {
        this._db.Add(history);
        await this._db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TicketHistory>> ListForTicketAsync(int workspaceId, int ticketId) => await this._db.Set<TicketHistory>()
            .Where(h => h.WorkspaceId == workspaceId && h.TicketId == ticketId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
}
