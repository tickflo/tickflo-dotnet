namespace Tickflo.Core.Services.Common;

using Tickflo.Core.Entities;

/// <summary>
/// Service for generating dashboard metrics, statistics, and visualizations.
/// Centralizes complex aggregation and calculation logic.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Calculates basic ticket statistics for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace to calculate stats for</param>
    /// <param name="userId">Current user (for scope filtering)</param>
    /// <param name="ticketViewScope">Scope filter: "all", "mine", or "team"</param>
    /// <param name="userTeamIds">Team IDs the user belongs to (for team scope)</param>
    /// <returns>Ticket statistics</returns>
    public Task<DashboardTicketStats> GetTicketStatsAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds);

    /// <summary>
    /// Generates activity time series data for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace</param>
    /// <param name="userId">Current user (for scope filtering)</param>
    /// <param name="ticketViewScope">Scope filter: "all", "mine", or "team"</param>
    /// <param name="userTeamIds">Team IDs the user belongs to</param>
    /// <param name="daysBack">Number of days to include in series</param>
    /// <returns>Daily activity counts</returns>
    public Task<List<ActivityDataPoint>> GetActivitySeriesAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds,
        int daysBack = 30);

    /// <summary>
    /// Gets top members by closed ticket count.
    /// </summary>
    /// <param name="workspaceId">The workspace</param>
    /// <param name="userId">Current user (for scope filtering)</param>
    /// <param name="ticketViewScope">Scope filter</param>
    /// <param name="userTeamIds">Team IDs the user belongs to</param>
    /// <param name="topN">Number of top members to return</param>
    /// <returns>Top members with their closed ticket counts</returns>
    public Task<List<TopMember>> GetTopMembersAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds,
        int topN = 5);

    /// <summary>
    /// Calculates average ticket resolution time.
    /// </summary>
    /// <param name="workspaceId">The workspace</param>
    /// <param name="userId">Current user (for scope filtering)</param>
    /// <param name="ticketViewScope">Scope filter</param>
    /// <param name="userTeamIds">Team IDs the user belongs to</param>
    /// <returns>Average resolution time formatted as string</returns>
    public Task<string> GetAverageResolutionTimeAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds);

    /// <summary>
    /// Gets ticket counts by priority.
    /// </summary>
    /// <param name="workspaceId">The workspace</param>
    /// <param name="userId">Current user (for scope filtering)</param>
    /// <param name="ticketViewScope">Scope filter</param>
    /// <param name="userTeamIds">Team IDs the user belongs to</param>
    /// <returns>Priority counts dictionary</returns>
    public Task<Dictionary<string, int>> GetPriorityCountsAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds);

    /// <summary>
    /// Filters tickets by assignment status.
    /// </summary>
    /// <param name="tickets">All tickets to filter</param>
    /// <param name="assignmentFilter">Filter: "unassigned", "me", "others", or "all"</param>
    /// <param name="currentUserId">Current user ID</param>
    /// <returns>Filtered ticket list</returns>
    public List<Ticket> FilterTicketsByAssignment(
        IEnumerable<Ticket> tickets,
        string assignmentFilter,
        int currentUserId);
}

/// <summary>
/// Dashboard ticket statistics.
/// </summary>
public class DashboardTicketStats
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ActiveMembers { get; set; }
}

/// <summary>
/// Time series data point for activity charts.
/// </summary>
public class ActivityDataPoint
{
    public string Date { get; set; } = string.Empty;
    public int Created { get; set; }
    public int Closed { get; set; }
}

/// <summary>
/// Top member by closed ticket count.
/// </summary>
public class TopMember
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClosedCount { get; set; }
}


