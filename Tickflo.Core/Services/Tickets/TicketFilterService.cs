namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Entities;

/// <summary>
/// Service for filtering and searching tickets based on multiple criteria.
/// </summary>
public class TicketFilterService : ITicketFilterService
{
    public List<Ticket> ApplyFilters(IEnumerable<Ticket> tickets, TicketFilterCriteria filter)
    {
        var filtered = tickets;

        // Text search filter
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var query = filter.Query.Trim().ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Subject ?? string.Empty).Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                (t.Description ?? string.Empty).Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }

        // Status filter (now by ID)
        if (filter.StatusId.HasValue)
        {
            filtered = filtered.Where(t => t.StatusId == filter.StatusId.Value);
        }

        // Priority filter (now by ID)
        if (filter.PriorityId.HasValue)
        {
            filtered = filtered.Where(t => t.PriorityId == filter.PriorityId.Value);
        }

        // Type filter (now by ID)
        if (filter.TypeId.HasValue)
        {
            filtered = filtered.Where(t => t.TicketTypeId == filter.TypeId.Value);
        }

        // Contact filter
        if (filter.ContactId.HasValue)
        {
            filtered = filtered.Where(t => t.ContactId == filter.ContactId.Value);
        }

        // Assignee user filter
        if (filter.AssigneeUserId.HasValue)
        {
            filtered = filtered.Where(t => t.AssignedUserId == filter.AssigneeUserId.Value);
        }

        // Team name filter (requires team resolution externally)
        // Note: This requires loading teams which should be done before calling
        // For now, filter by the stored name if teams have been pre-loaded
        // This is a limitation - might need to pass team ID instead

        // Location filter
        if (filter.LocationId.HasValue)
        {
            filtered = filtered.Where(t => t.LocationId == filter.LocationId.Value);
        }

        // "Mine" filter
        if (filter.Mine && filter.CurrentUserId.HasValue)
        {
            filtered = filtered.Where(t => t.AssignedUserId == filter.CurrentUserId.Value);
        }

        return [.. filtered];
    }

    public List<Ticket> ApplyScopeFilter(
        IEnumerable<Ticket> tickets,
        int userId,
        string scope,
        List<int> userTeamIds)
    {
        var filtered = tickets;

        var normalizedScope = scope?.ToLowerInvariant() ?? "all";

        switch (normalizedScope)
        {
            case "mine":
                filtered = filtered.Where(t => t.AssignedUserId == userId);
                break;
            case "team":
                var teamIds = userTeamIds.ToHashSet();
                filtered = filtered.Where(t =>
                    t.AssignedUserId == userId ||
                    (t.AssignedTeamId.HasValue && teamIds.Contains(t.AssignedTeamId.Value)));
                break;
            default:
                break;
                // "all" - no filter
        }

        return [.. filtered];
    }

    public int CountMyTickets(IEnumerable<Ticket> tickets, int userId) => tickets.Count(t => t.AssignedUserId == userId);

    public int? ResolveStatusId(string? statusName, IReadOnlyList<TicketStatus> statuses)
    {
        if (string.IsNullOrWhiteSpace(statusName) || statusName.Equals(TicketFilterConstants.OpenStatusFilter, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return statuses.FirstOrDefault(s => s.Name.Equals(statusName.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public int? ResolvePriorityId(string? priorityName, IReadOnlyList<TicketPriority> priorities)
    {
        if (string.IsNullOrWhiteSpace(priorityName))
        {
            return null;
        }

        return priorities.FirstOrDefault(p => p.Name.Equals(priorityName.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public int? ResolveTypeId(string? typeName, IReadOnlyList<TicketType> types)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        return types.FirstOrDefault(t => t.Name.Equals(typeName.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public List<Ticket> ApplyOpenStatusFilter(IEnumerable<Ticket> tickets, IReadOnlyList<TicketStatus> statuses)
    {
        var closedStatusIds = statuses
            .Where(s => s.IsClosedState)
            .Select(s => s.Id)
            .ToHashSet();

        return [.. tickets.Where(t => !t.StatusId.HasValue || !closedStatusIds.Contains(t.StatusId.Value))];
    }

    public List<Ticket> ApplyContactFilter(IEnumerable<Ticket> tickets, string? contactQuery, Dictionary<int, Contact> contactsById)
    {
        if (string.IsNullOrWhiteSpace(contactQuery))
        {
            return [.. tickets];
        }

        var query = contactQuery.Trim();
        return [.. tickets.Where(t => TicketMatchesContactQuery(t, query, contactsById))];
    }

    private static bool TicketMatchesContactQuery(Ticket ticket, string query, Dictionary<int, Contact> contactsById)
    {
        if (!ticket.ContactId.HasValue || !contactsById.TryGetValue(ticket.ContactId.Value, out var contact))
        {
            return false;
        }

        return (contact.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (contact.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public List<Ticket> ApplyTeamFilter(IEnumerable<Ticket> tickets, string? teamName, Dictionary<int, Team> teamsById)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return [.. tickets];
        }

        var team = teamsById.Values.FirstOrDefault(t =>
            string.Equals(t.Name, teamName.Trim(), StringComparison.OrdinalIgnoreCase));

        return team != null
            ? [.. tickets.Where(t => t.AssignedTeamId == team.Id)]
            : [];
    }

    public List<Ticket> Paginate(IEnumerable<Ticket> tickets, int pageNumber, int pageSize)
    {
        var normalizedPageSize = pageSize <= 0 ? TicketFilterConstants.DefaultPageSize : Math.Min(pageSize, TicketFilterConstants.MaxPageSize);
        var normalizedPageNumber = pageNumber <= 0 ? TicketFilterConstants.MinPageNumber : pageNumber;

        var startIndex = (normalizedPageNumber - 1) * normalizedPageSize;
        return [.. tickets.Skip(startIndex).Take(normalizedPageSize)];
    }
}


