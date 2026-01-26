namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public interface ITicketHistoryRepository
{
    public Task CreateAsync(TicketHistory history);
    public Task<IReadOnlyList<TicketHistory>> ListForTicketAsync(int workspaceId, int ticketId);
}


public class TicketHistoryRepository(TickfloDbContext dbContext) : ITicketHistoryRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task CreateAsync(TicketHistory history)
    {
        this.dbContext.Add(history);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TicketHistory>> ListForTicketAsync(int workspaceId, int ticketId) => await this.dbContext.Set<TicketHistory>()
            .Where(h => h.WorkspaceId == workspaceId && h.TicketId == ticketId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
}
