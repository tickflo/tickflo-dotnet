using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Contacts;

public interface IContactListingService
{
    /// <summary>
    /// Gets filtered contacts for a workspace with optional priority and search filtering.
    /// </summary>
    Task<(IReadOnlyList<Contact> Items, IReadOnlyList<TicketPriority> Priorities)> GetListAsync(
        int workspaceId, 
        string? priorityFilter = null, 
        string? searchQuery = null);
}

