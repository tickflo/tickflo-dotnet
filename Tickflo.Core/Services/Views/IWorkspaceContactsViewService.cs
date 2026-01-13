using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public interface IWorkspaceContactsViewService
{
    Task<WorkspaceContactsViewData> BuildAsync(int workspaceId, int userId, string? priorityFilter = null, string? searchQuery = null);
}

public class WorkspaceContactsViewData
{
    public List<Contact> Contacts { get; set; } = new();
    public List<TicketPriority> Priorities { get; set; } = new();
    public Dictionary<string, string> PriorityColorByName { get; set; } = new();
    public bool CanCreateContacts { get; set; }
    public bool CanEditContacts { get; set; }
}


