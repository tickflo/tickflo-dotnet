using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly TickfloDbContext _db;
    public TicketHistoryRepository(TickfloDbContext db) { _db = db; }

    public async Task CreateAsync(TicketHistory history)
    {
        _db.Add(history);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TicketHistory>> ListForTicketAsync(int workspaceId, int ticketId)
    {
        return await _db.Set<TicketHistory>()
            .Where(h => h.WorkspaceId == workspaceId && h.TicketId == ticketId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }
}