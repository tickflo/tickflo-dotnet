using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITicketStatusRepository
{
    Task<IReadOnlyList<TicketStatus>> ListAsync(int workspaceId, CancellationToken ct = default);
    Task<TicketStatus?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default);
    Task<TicketStatus?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default);
    Task<TicketStatus?> FindByIsClosedStateAsync(int workspaceId, bool isClosedState, CancellationToken ct = default);
    Task<TicketStatus> CreateAsync(TicketStatus status, CancellationToken ct = default);
    Task<TicketStatus> UpdateAsync(TicketStatus status, CancellationToken ct = default);
    Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}
