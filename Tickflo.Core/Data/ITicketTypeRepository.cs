namespace Tickflo.Core.Data;

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
