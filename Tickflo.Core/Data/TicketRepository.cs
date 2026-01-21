namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TicketRepository(TickfloDbContext dbContext) : ITicketRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<IReadOnlyList<Ticket>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.TicketInventories)
            .ThenInclude(ti => ti.Inventory)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<Ticket?> FindAsync(int workspaceId, int id, CancellationToken ct = default)
        => await this.dbContext.Tickets
            .Include(t => t.TicketInventories)
            .ThenInclude(ti => ti.Inventory)
            .FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == id, ct);


    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default)
    {
        this.dbContext.Tickets.Add(ticket);
        await this.dbContext.SaveChangesAsync(ct);

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
            this.dbContext.TicketInventories.Add(inventory);
        }

        await this.dbContext.SaveChangesAsync(ct);
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        this.dbContext.Tickets.Update(ticket);
        await this.SyncTicketInventoriesAsync(ticket);
        await this.dbContext.SaveChangesAsync(ct);

        return ticket;
    }

    private async Task SyncTicketInventoriesAsync(Ticket ticket)
    {
        await this.RemoveDeletedInventoriesAsync(ticket);
        await this.AddOrUpdateInventoriesAsync(ticket);
    }

    private async Task RemoveDeletedInventoriesAsync(Ticket ticket)
    {
        var existingInventories = this.dbContext.TicketInventories
            .Where(ti => ti.TicketId == ticket.Id)
            .ToList();

        var currentInventoryIds = ticket.TicketInventories
            .Select(ti => ti.Id)
            .ToHashSet();

        var inventoriesToRemove = existingInventories
            .Where(existing => !currentInventoryIds.Contains(existing.Id));

        foreach (var inventory in inventoriesToRemove)
        {
            this.dbContext.TicketInventories.Remove(inventory);
        }
    }

    private async Task AddOrUpdateInventoriesAsync(Ticket ticket)
    {
        foreach (var inventory in ticket.TicketInventories)
        {
            inventory.TicketId = ticket.Id;

            if (IsNewInventory(inventory))
            {
                this.dbContext.TicketInventories.Add(inventory);
            }
            else
            {
                this.dbContext.TicketInventories.Update(inventory);
            }
        }
    }

    private static bool IsNewInventory(TicketInventory inventory) => inventory.Id == 0;
}
