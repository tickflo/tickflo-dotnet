namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TicketStatusRepository(TickfloDbContext dbContext) : ITicketStatusRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<IReadOnlyList<TicketStatus>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await this.dbContext.TicketStatuses.Where(s => s.WorkspaceId == workspaceId).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);

    public async Task<TicketStatus?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default)
        => await this.dbContext.TicketStatuses.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Id == id, ct);

    public async Task<TicketStatus?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default)
        => await this.dbContext.TicketStatuses.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Name == name, ct);

    public async Task<TicketStatus?> FindByIsClosedStateAsync(int workspaceId, bool isClosedState, CancellationToken ct = default)
        => await this.dbContext.TicketStatuses.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.IsClosedState == isClosedState, ct);

    public async Task<TicketStatus> CreateAsync(TicketStatus status, CancellationToken ct = default)
    {
        this.dbContext.TicketStatuses.Add(status);
        await this.dbContext.SaveChangesAsync(ct);
        return status;
    }

    public async Task<TicketStatus> UpdateAsync(TicketStatus status, CancellationToken ct = default)
    {
        this.dbContext.TicketStatuses.Update(status);
        await this.dbContext.SaveChangesAsync(ct);
        return status;
    }

    public async Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await this.FindByIdAsync(workspaceId, id, ct);
        if (entity != null)
        {
            this.dbContext.TicketStatuses.Remove(entity);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }
}
