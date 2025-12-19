using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TicketRepository(TickfloDbContext db) : ITicketRepository
{
    public async Task<IReadOnlyList<Ticket>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await db.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.TicketInventories)
            .ThenInclude(ti => ti.Inventory)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<Ticket?> FindAsync(int workspaceId, int id, CancellationToken ct = default)
        => await db.Tickets
            .Include(t => t.TicketInventories)
            .ThenInclude(ti => ti.Inventory)
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == id, ct);


    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        // Set TicketId for new TicketInventories and save
        if (ticket.TicketInventories != null && ticket.TicketInventories.Count > 0)
        {
            foreach (var ti in ticket.TicketInventories)
            {
                ti.TicketId = ticket.Id;
                db.TicketInventories.Add(ti);
            }
            await db.SaveChangesAsync(ct);
        }
        return ticket;
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Update(ticket);
        // Remove TicketInventories that are no longer present
        var existing = db.TicketInventories.Where(ti => ti.TicketId == ticket.Id).ToList();
        var updatedIds = ticket.TicketInventories.Select(ti => ti.Id).ToHashSet();
        foreach (var old in existing)
        {
            if (!updatedIds.Contains(old.Id))
                db.TicketInventories.Remove(old);
        }
        // Add or update current TicketInventories
        foreach (var ti in ticket.TicketInventories)
        {
            ti.TicketId = ticket.Id;
            if (ti.Id == 0)
                db.TicketInventories.Add(ti);
            else
                db.TicketInventories.Update(ti);
        }
        await db.SaveChangesAsync(ct);
        return ticket;
    }
}