namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

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

        await this.AddTicketInventoriesAsync(ticket, ct);

        return ticket;
    }

    private async Task AddTicketInventoriesAsync(Ticket ticket, CancellationToken ct)
    {
        if (ticket.TicketInventories == null || ticket.TicketInventories.Count == 0)
        {
            return;
        }

        foreach (var inventory in ticket.TicketInventories)
        {
            inventory.TicketId = ticket.Id;
            db.TicketInventories.Add(inventory);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Update(ticket);
        await this.SyncTicketInventoriesAsync(ticket, ct);
        await db.SaveChangesAsync(ct);

        return ticket;
    }

    private async Task SyncTicketInventoriesAsync(Ticket ticket, CancellationToken ct)
    {
        await this.RemoveDeletedInventoriesAsync(ticket);
        await this.AddOrUpdateInventoriesAsync(ticket);
    }

    private async Task RemoveDeletedInventoriesAsync(Ticket ticket)
    {
        var existingInventories = db.TicketInventories
            .Where(ti => ti.TicketId == ticket.Id)
            .ToList();

        var currentInventoryIds = ticket.TicketInventories
            .Select(ti => ti.Id)
            .ToHashSet();

        var inventoriesToRemove = existingInventories
            .Where(existing => !currentInventoryIds.Contains(existing.Id));

        foreach (var inventory in inventoriesToRemove)
        {
            db.TicketInventories.Remove(inventory);
        }
    }

    private async Task AddOrUpdateInventoriesAsync(Ticket ticket)
    {
        foreach (var inventory in ticket.TicketInventories)
        {
            inventory.TicketId = ticket.Id;

            if (IsNewInventory(inventory))
            {
                db.TicketInventories.Add(inventory);
            }
            else
            {
                db.TicketInventories.Update(inventory);
            }
        }
    }

    private static bool IsNewInventory(TicketInventory inventory) => inventory.Id == 0;
}
