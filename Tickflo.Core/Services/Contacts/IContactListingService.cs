namespace Tickflo.Core.Services.Contacts;

using Tickflo.Core.Entities;

public interface IContactListingService
{
    /// <summary>
    /// Gets filtered contacts for a workspace with optional priority and search filtering.
    /// </summary>
    public Task<(IReadOnlyList<Contact> Items, IReadOnlyList<TicketPriority> Priorities)> GetListAsync(
        int workspaceId,
        string? priorityFilter = null,
        string? searchQuery = null);
}

