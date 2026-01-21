namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITicketStatusRepository
{
    public Task<IReadOnlyList<TicketStatus>> ListAsync(int workspaceId, CancellationToken ct = default);
    public Task<TicketStatus?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default);
    public Task<TicketStatus?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default);
    public Task<TicketStatus?> FindByIsClosedStateAsync(int workspaceId, bool isClosedState, CancellationToken ct = default);
    public Task<TicketStatus> CreateAsync(TicketStatus status, CancellationToken ct = default);
    public Task<TicketStatus> UpdateAsync(TicketStatus status, CancellationToken ct = default);
    public Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}
