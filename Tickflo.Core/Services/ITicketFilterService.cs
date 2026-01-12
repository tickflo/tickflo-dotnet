using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

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
    List<Ticket> ApplyFilters(IEnumerable<Ticket> tickets, TicketFilterCriteria filter);

    /// <summary>
    /// Applies role-based scope filtering (all/mine/team).
    /// </summary>
    /// <param name="tickets">Source tickets</param>
    /// <param name="userId">Current user</param>
    /// <param name="scope">Scope: "all", "mine", or "team"</param>
    /// <param name="userTeamIds">Team IDs user belongs to</param>
    /// <returns>Filtered tickets</returns>
    List<Ticket> ApplyScopeFilter(
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
    int CountMyTickets(IEnumerable<Ticket> tickets, int userId);
}

/// <summary>
/// Comprehensive ticket filter criteria.
/// </summary>
public class TicketFilterCriteria
{
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Type { get; set; }
    public int? ContactId { get; set; }
    public int? AssigneeUserId { get; set; }
    public string? AssigneeTeamName { get; set; }
    public int? LocationId { get; set; }
    public bool Mine { get; set; }
    public int? CurrentUserId { get; set; }
}
