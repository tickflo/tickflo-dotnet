namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public interface IWorkspaceContactsViewService
{
    public Task<WorkspaceContactsViewData> BuildAsync(int workspaceId, int userId, string? priorityFilter = null, string? searchQuery = null);
}

public class WorkspaceContactsViewData
{
    public List<Contact> Contacts { get; set; } = [];
    public List<TicketPriority> Priorities { get; set; } = [];
    public Dictionary<string, string> PriorityColorByName { get; set; } = [];
    public bool CanCreateContacts { get; set; }
    public bool CanEditContacts { get; set; }
}


