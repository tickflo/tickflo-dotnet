namespace Tickflo.Core.Services.Contacts;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class ContactListingService(
    IContactRepository contactRepository,
    ITicketPriorityRepository priorityRepository) : IContactListingService
{
    private readonly IContactRepository contactRepository = contactRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;

    public async Task<(IReadOnlyList<Contact> Items, IReadOnlyList<TicketPriority> Priorities)> GetListAsync(
        int workspaceId,
        string? priorityFilter = null,
        string? searchQuery = null)
    {
        var allContacts = await this.contactRepository.ListAsync(workspaceId);
        var filtered = FilterContacts(allContacts, priorityFilter, searchQuery);
        var priorities = await this.priorityRepository.ListAsync(workspaceId);

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
            var trimmedQuery = searchQuery.Trim();
            result = result.Where(c =>
                (!string.IsNullOrWhiteSpace(c.Name) && c.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Email) && c.Email.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.Company) && c.Company.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            );
        }

        return result;
    }
}

