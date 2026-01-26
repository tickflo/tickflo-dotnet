namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Entities;

/// <summary>
/// Service for filtering and searching tickets based on multiple criteria.
/// </summary>

/// <summary>
/// Service for filtering and searching tickets based on multiple criteria.
/// </summary>
public interface ITicketFilterService
{
    /// <summary>
    /// Applies comprehensive filters to a ticket list.
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Filtered tickets</returns>
    public List<Ticket> ApplyFilters(IEnumerable<Ticket> tickets, TicketFilterCriteria filter);

    /// <summary>
    /// Applies role-based scope filtering (all/mine/team).
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="userId">Current user</param>
    /// <param name="scope">Scope: "all", "mine", or "team"</param>
    /// <param name="userTeamIds">Team IDs user belongs to</param>
    /// <returns>Filtered tickets</returns>
    public List<Ticket> ApplyScopeFilter(
        IEnumerable<Ticket> tickets,
        int userId,
        string scope,
        List<int> userTeamIds);

    /// <summary>
    /// Counts tickets matching "mine" criteria.
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="userId">Current user</param>
    /// <returns>Count of tickets assigned to user</returns>
    public int CountMyTickets(IEnumerable<Ticket> tickets, int userId);

    /// <summary>
    /// Resolves a status name to its ID. Returns null if the name is "Open" (special filter) or not found.
    /// </summary>
    /// <param name="statusName">Status name to resolve</param>
    /// <param name="statuses">Available statuses</param>
    /// <returns>Status ID or null</returns>
    public int? ResolveStatusId(string? statusName, IReadOnlyList<TicketStatus> statuses);

    /// <summary>
    /// Resolves a priority name to its ID.
    /// </summary>
    /// <param name="priorityName">Priority name to resolve</param>
    /// <param name="priorities">Available priorities</param>
    /// <returns>Priority ID or null</returns>
    public int? ResolvePriorityId(string? priorityName, IReadOnlyList<TicketPriority> priorities);

    /// <summary>
    /// Resolves a type name to its ID.
    /// </summary>
    /// <param name="typeName">Type name to resolve</param>
    /// <param name="types">Available types</param>
    /// <returns>Type ID or null</returns>
    public int? ResolveTypeId(string? typeName, IReadOnlyList<TicketType> types);

    /// <summary>
    /// Filters tickets by "Open" status (excludes closed states).
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="statuses">Available statuses with closure state</param>
    /// <returns>Filtered tickets excluding closed states</returns>
    public List<Ticket> ApplyOpenStatusFilter(IEnumerable<Ticket> tickets, IReadOnlyList<TicketStatus> statuses);

    /// <summary>
    /// Filters tickets by contact search query (name or email).
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="contactQuery">Search query</param>
    /// <param name="contactsById">Lookup dictionary for contacts</param>
    /// <returns>Filtered tickets</returns>
    public List<Ticket> ApplyContactFilter(IEnumerable<Ticket> tickets, string? contactQuery, Dictionary<int, Contact> contactsById);

    /// <summary>
    /// Filters tickets by team name.
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="teamName">Team name to filter by</param>
    /// <param name="teamsById">Lookup dictionary for teams</param>
    /// <returns>Filtered tickets</returns>
    public List<Ticket> ApplyTeamFilter(IEnumerable<Ticket> tickets, string? teamName, Dictionary<int, Team> teamsById);

    /// <summary>
    /// Paginates a list of tickets.
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated tickets</returns>
    public List<Ticket> Paginate(IEnumerable<Ticket> tickets, int pageNumber, int pageSize);
}

/// <summary>
/// Comprehensive ticket filter criteria.
/// </summary>
public class TicketFilterCriteria
{
    public string? Query { get; set; }
    public int? StatusId { get; set; }
    public int? PriorityId { get; set; }
    public int? TypeId { get; set; }
    public int? ContactId { get; set; }
    public int? AssigneeUserId { get; set; }
    public string? AssigneeTeamName { get; set; }
    public int? LocationId { get; set; }
    public bool Mine { get; set; }
    public int? CurrentUserId { get; set; }
}

/// <summary>
/// Constants for ticket filtering.
/// </summary>
public static class TicketFilterConstants
{
    public const string OpenStatusFilter = "Open";
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 200;
    public const int MinPageNumber = 1;
}

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


