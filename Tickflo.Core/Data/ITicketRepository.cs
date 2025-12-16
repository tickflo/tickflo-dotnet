using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITicketRepository
{
    Task<IReadOnlyList<Ticket>> ListAsync(int workspaceId, CancellationToken ct = default);
    Task<Ticket?> FindAsync(int workspaceId, int id, CancellationToken ct = default);
    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default);
    Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default);
}