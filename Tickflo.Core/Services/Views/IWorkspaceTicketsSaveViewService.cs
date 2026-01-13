using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public class WorkspaceTicketsSaveViewData
{
    public bool CanCreateTickets { get; set; }
    public bool CanEditTickets { get; set; }
    public bool CanAccessTicket { get; set; }
}

public interface IWorkspaceTicketsSaveViewService
{
    Task<WorkspaceTicketsSaveViewData> BuildAsync(int workspaceId, int userId, bool isNew, Ticket? existing = null);
}


