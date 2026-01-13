using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITicketHistoryRepository
{
    Task CreateAsync(TicketHistory history);
    Task<IReadOnlyList<TicketHistory>> ListForTicketAsync(int workspaceId, int ticketId);
}