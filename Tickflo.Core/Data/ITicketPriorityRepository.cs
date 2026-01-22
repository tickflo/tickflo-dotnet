namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITicketPriorityRepository
{
    public Task<IReadOnlyList<TicketPriority>> ListAsync(int workspaceId, CancellationToken ct = default);
    public Task<TicketPriority?> FindAsync(int workspaceId, string name, CancellationToken ct = default);
    public Task<TicketPriority> CreateAsync(TicketPriority priority, CancellationToken ct = default);
    public Task<TicketPriority> UpdateAsync(TicketPriority priority, CancellationToken ct = default);
    public Task<bool> DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}
