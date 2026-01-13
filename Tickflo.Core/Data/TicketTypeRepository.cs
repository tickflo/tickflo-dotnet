using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class TicketTypeRepository(TickfloDbContext db) : ITicketTypeRepository
{
    public async Task<IReadOnlyList<TicketType>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await db.TicketTypes.Where(t => t.WorkspaceId == workspaceId).OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);

    public async Task<TicketType?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default)
        => await db.TicketTypes.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == id, ct);

    public async Task<TicketType?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default)
        => await db.TicketTypes.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name == name, ct);

    public async Task<TicketType> CreateAsync(TicketType type, CancellationToken ct = default)
    {
        db.TicketTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return type;
    }

    public async Task<TicketType> UpdateAsync(TicketType type, CancellationToken ct = default)
    {
        db.TicketTypes.Update(type);
        await db.SaveChangesAsync(ct);
        return type;
    }

    public async Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(workspaceId, id, ct);
        if (entity != null)
        {
            db.TicketTypes.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }
}
