namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITicketRepository
{
    public Task<IReadOnlyList<Ticket>> ListAsync(int workspaceId, CancellationToken ct = default);
    public Task<Ticket?> FindAsync(int workspaceId, int id, CancellationToken ct = default);
    public Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default);
    public Task<Ticket> UpdateAsync(Ticket ticket, CancellationToken ct = default);
}
