using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITicketPriorityRepository
{
    Task<IReadOnlyList<TicketPriority>> ListAsync(int workspaceId, CancellationToken ct = default);
    Task<TicketPriority?> FindAsync(int workspaceId, string name, CancellationToken ct = default);
    Task<TicketPriority> CreateAsync(TicketPriority priority, CancellationToken ct = default);
    Task<TicketPriority> UpdateAsync(TicketPriority priority, CancellationToken ct = default);
    Task<bool> DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}
