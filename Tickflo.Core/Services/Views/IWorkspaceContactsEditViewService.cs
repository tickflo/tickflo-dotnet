namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceContactsEditViewData
{
    public bool CanViewContacts { get; set; }
    public bool CanEditContacts { get; set; }
    public bool CanCreateContacts { get; set; }
    public Contact? ExistingContact { get; set; }
    public List<TicketPriority> Priorities { get; set; } = [];
}

public interface IWorkspaceContactsEditViewService
{
    public Task<WorkspaceContactsEditViewData> BuildAsync(int workspaceId, int userId, int contactId = 0);
}


