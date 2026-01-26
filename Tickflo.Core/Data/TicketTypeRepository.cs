namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public interface ITicketTypeRepository
{
    public Task<IReadOnlyList<TicketType>> ListAsync(int workspaceId, CancellationToken ct = default);
    public Task<TicketType?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default);
    public Task<TicketType?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default);
    public Task<TicketType> CreateAsync(TicketType type, CancellationToken ct = default);
    public Task<TicketType> UpdateAsync(TicketType type, CancellationToken ct = default);
    public Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}


public class TicketTypeRepository(TickfloDbContext dbContext) : ITicketTypeRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<IReadOnlyList<TicketType>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await this.dbContext.TicketTypes.Where(t => t.WorkspaceId == workspaceId).OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);

    public async Task<TicketType?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default)
        => await this.dbContext.TicketTypes.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == id, ct);

    public async Task<TicketType?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default)
        => await this.dbContext.TicketTypes.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name == name, ct);

    public async Task<TicketType> CreateAsync(TicketType type, CancellationToken ct = default)
    {
        this.dbContext.TicketTypes.Add(type);
        await this.dbContext.SaveChangesAsync(ct);
        return type;
    }

    public async Task<TicketType> UpdateAsync(TicketType type, CancellationToken ct = default)
    {
        this.dbContext.TicketTypes.Update(type);
        await this.dbContext.SaveChangesAsync(ct);
        return type;
    }

    public async Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await this.FindByIdAsync(workspaceId, id, ct);
        if (entity != null)
        {
            this.dbContext.TicketTypes.Remove(entity);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }
}
