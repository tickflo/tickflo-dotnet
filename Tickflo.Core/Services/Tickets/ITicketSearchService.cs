namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Entities;

/// <summary>
/// Search filter criteria for ticket queries.
/// </summary>
public class TicketSearchCriteria
{
    public int? AssignedToUserId { get; set; }
    public int? AssignedToTeamId { get; set; }
    public string? Status { get; set; }
    public int? StatusId { get; set; }
    public string? Priority { get; set; }
    public int? PriorityId { get; set; }
    public string? Type { get; set; }
    public int? TypeId { get; set; }
    public int? ContactId { get; set; }
    public int? LocationId { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }
    public string? SearchTerm { get; set; } // Search in subject and description
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
}

/// <summary>
/// Result DTO for ticket search with pagination.
/// </summary>
public class TicketSearchResult
{
    public List<Ticket> Tickets { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (this.TotalCount + this.PageSize - 1) / this.PageSize;
}

/// <summary>
/// Behavior-focused service for ticket search, filtering, and discovery.
/// Handles complex query scenarios, reporting-ready data, and performance optimization.
/// </summary>
public interface ITicketSearchService
{
    /// <summary>
    /// Search tickets using flexible criteria with pagination.
    /// Respects user's workspace and applies appropriate filters.
    /// </summary>
    public Task<TicketSearchResult> SearchAsync(
        int workspaceId,
        TicketSearchCriteria criteria,
        int requestingUserId);

    /// <summary>
    /// Get all tickets assigned to a specific user.
    /// Includes both directly assigned and team-assigned tickets.
    /// </summary>
    public Task<List<Ticket>> GetMyTicketsAsync(
        int workspaceId,
        int userId,
        string? statusFilter = null);

    /// <summary>
    /// Get all open/active tickets in the workspace.
    /// Used for dashboards and monitoring.
    /// </summary>
    public Task<List<Ticket>> GetActiveTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null);

    /// <summary>
    /// Get recently updated tickets for activity feed.
    /// </summary>
    public Task<List<Ticket>> GetRecentlyUpdatedAsync(
        int workspaceId,
        int limitToLastDays = 7,
        int take = 20);

    /// <summary>
    /// Get high-priority tickets that need attention.
    /// Useful for SLA monitoring and escalation.
    /// </summary>
    public Task<List<Ticket>> GetHighPriorityTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null);

    /// <summary>
    /// Get tickets for a specific contact.
    /// Returns all tickets related to a contact in any role.
    /// </summary>
    public Task<List<Ticket>> GetContactTicketsAsync(
        int workspaceId,
        int contactId);

    /// <summary>
    /// Get unassigned tickets awaiting assignment.
    /// Useful for queue management and dispatching.
    /// </summary>
    public Task<List<Ticket>> GetUnassignedTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null);

    /// <summary>
    /// Get tickets approaching their SLA deadline.
    /// </summary>
    public Task<List<Ticket>> GetSLAAtRiskAsync(
        int workspaceId,
        int hoursUntilDueWarning = 24);

    /// <summary>
    /// Get tickets with a specific tag/label.
    /// </summary>
    public Task<List<Ticket>> GetByTagAsync(
        int workspaceId,
        string tag);

    /// <summary>
    /// Get bulk ticket data for reporting/export.
    /// Includes related data in denormalized format for performance.
    /// </summary>
    public Task<List<Dictionary<string, object>>> GetBulkDataForExportAsync(
        int workspaceId,
        TicketSearchCriteria criteria);
}
