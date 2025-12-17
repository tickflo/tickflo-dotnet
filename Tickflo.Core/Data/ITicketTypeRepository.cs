using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITicketTypeRepository
{
    Task<IReadOnlyList<TicketType>> ListAsync(int workspaceId, CancellationToken ct = default);
    Task<TicketType?> FindByIdAsync(int workspaceId, int id, CancellationToken ct = default);
    Task<TicketType?> FindByNameAsync(int workspaceId, string name, CancellationToken ct = default);
    Task<TicketType> CreateAsync(TicketType type, CancellationToken ct = default);
    Task<TicketType> UpdateAsync(TicketType type, CancellationToken ct = default);
    Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}
