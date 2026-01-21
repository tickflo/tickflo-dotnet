namespace Tickflo.Core.Services.Contacts;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class ContactListingService(
    IContactRepository contactRepo,
    ITicketPriorityRepository priorityRepo) : IContactListingService
{
    private readonly IContactRepository _contactRepo = contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;

    public async Task<(IReadOnlyList<Contact> Items, IReadOnlyList<TicketPriority> Priorities)> GetListAsync(
        int workspaceId,
        string? priorityFilter = null,
        string? searchQuery = null)
    {
        var allContacts = await this._contactRepo.ListAsync(workspaceId);
        var filtered = FilterContacts(allContacts, priorityFilter, searchQuery);
        var priorities = await this._priorityRepo.ListAsync(workspaceId);

        return (filtered.ToList(), priorities.ToList());
    }

    private static IEnumerable<Contact> FilterContacts(
        IEnumerable<Contact> contacts,
        string? priorityFilter,
        string? searchQuery)
    {
        var result = contacts;

        if (!string.IsNullOrWhiteSpace(priorityFilter))
        {
            result = result.Where(c =>
                string.Equals(c.Priority, priorityFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var q = searchQuery.Trim();
            result = result.Where(c =>
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Email) && c.Email.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Company) && c.Company.Contains(q, StringComparison.OrdinalIgnoreCase))
            );
        }

        return result;
    }
}

