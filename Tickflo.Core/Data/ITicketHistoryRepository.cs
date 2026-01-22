namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITicketHistoryRepository
{
    public Task CreateAsync(TicketHistory history);
    public Task<IReadOnlyList<TicketHistory>> ListForTicketAsync(int workspaceId, int ticketId);
}
