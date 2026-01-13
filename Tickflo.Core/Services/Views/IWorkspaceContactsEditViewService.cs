using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public class WorkspaceContactsEditViewData
{
    public bool CanViewContacts { get; set; }
    public bool CanEditContacts { get; set; }
    public bool CanCreateContacts { get; set; }
    public Contact? ExistingContact { get; set; }
    public List<TicketPriority> Priorities { get; set; } = new();
}

public interface IWorkspaceContactsEditViewService
{
    Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0);
}


