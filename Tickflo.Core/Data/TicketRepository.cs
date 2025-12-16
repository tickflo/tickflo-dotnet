using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TicketRepository(TickfloDbContext db) : ITicketRepository
{
    public async Task<IReadOnlyList<Ticket>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await db.Tickets.Where(t => t.WorkspaceId == workspaceId).OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

    public async Task<Ticket?> FindAsync(int workspaceId, int id, CancellationToken ct = default)
        => await db.Tickets.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == id, ct);

    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Update(ticket);
        await db.SaveChangesAsync(ct);
        return ticket;
    }
}