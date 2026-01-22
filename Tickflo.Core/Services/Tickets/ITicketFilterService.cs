namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Entities;

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


