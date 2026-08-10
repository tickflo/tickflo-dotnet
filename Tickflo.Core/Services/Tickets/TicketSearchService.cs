namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of ticket search and discovery service.
/// Optimized for complex queries and reporting scenarios.
/// </summary>

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

public class TicketSearchService(TickfloDbContext dbContext) : ITicketSearchService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<TicketSearchResult> SearchAsync(
        int workspaceId,
        TicketSearchCriteria criteria,
        int requestingUserId)
    {
        // Validation: User has access to workspace
        var userAccess = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == requestingUserId && uw.WorkspaceId == workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        // Compose query at the database level — no in-memory loading
        var query = this.dbContext.Tickets.Where(t => t.WorkspaceId == workspaceId);

        // Apply filters at the database level
        query = ApplyFilters(query, criteria);

        // Get total count before pagination (single DB round-trip)
        var total = await query.CountAsync();

        // Apply pagination at the database level
        var skip = (criteria.PageNumber - 1) * criteria.PageSize;
        var tickets = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToListAsync();

        return new TicketSearchResult
        {
            Tickets = tickets,
            TotalCount = total,
            PageNumber = criteria.PageNumber,
            PageSize = criteria.PageSize
        };
    }

    public async Task<List<Ticket>> GetMyTicketsAsync(
        int workspaceId,
        int userId,
        string? statusFilter = null)
    {
        // Preload team memberships into a HashSet to avoid sync-over-async deadlock
        // in filter lambdas (#143 fix)
        var teamIds = await this.dbContext.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync();
        var teamIdSet = new HashSet<int>(teamIds);

        var query = this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Where(t => t.AssignedUserId == userId ||
                       (t.AssignedTeamId.HasValue && teamIdSet.Contains(t.AssignedTeamId.Value)));

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var statusId = await this.dbContext.TicketStatuses
                .Where(s => s.WorkspaceId == workspaceId && s.Name.ToLower() == statusFilter.ToLower())
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
            if (statusId.HasValue)
            {
                query = query.Where(t => t.StatusId == statusId.Value);
            }
        }

        return await query.ToListAsync();
    }

    public async Task<List<Ticket>> GetActiveTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var closedIds = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId && s.IsClosedState)
            .Select(s => (int?)s.Id)
            .ToListAsync();

        var query = this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Where(t => !t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value));

        if (limitToTeamId.HasValue)
        {
            query = query.Where(t => t.AssignedTeamId == limitToTeamId);
        }

        return await query.ToListAsync();
    }

    public async Task<List<Ticket>> GetRecentlyUpdatedAsync(
        int workspaceId,
        int limitToLastDays = 7,
        int take = 20)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-limitToLastDays);

        return await this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value >= cutoffDate)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetHighPriorityTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var highPriorityIds = await this.dbContext.TicketPriorities
            .Where(p => p.WorkspaceId == workspaceId && (p.Name == "Critical" || p.Name == "High"))
            .Select(p => (int?)p.Id)
            .ToListAsync();

        var closedIds = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId && s.IsClosedState)
            .Select(s => (int?)s.Id)
            .ToListAsync();

        var query = this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Where(t => t.PriorityId.HasValue && highPriorityIds.Contains(t.PriorityId.Value))
            .Where(t => !t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value));

        if (limitToTeamId.HasValue)
        {
            query = query.Where(t => t.AssignedTeamId == limitToTeamId);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<Ticket>> GetContactTicketsAsync(
        int workspaceId,
        int contactId)
    {
        return await this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId && t.ContactId == contactId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetUnassignedTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var closedIds = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId && s.IsClosedState)
            .Select(s => (int?)s.Id)
            .ToListAsync();

        var query = this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Where(t => t.AssignedUserId == null && t.AssignedTeamId == null)
            .Where(t => !t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value));

        if (limitToTeamId.HasValue)
        {
            query = query.Where(t => t.AssignedTeamId == limitToTeamId);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<List<Ticket>> GetSLAAtRiskAsync(
        int workspaceId,
        int hoursUntilDueWarning = 24)
    {
        var closedIds = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId && s.IsClosedState)
            .Select(s => (int?)s.Id)
            .ToListAsync();

        var warningThreshold = DateTime.UtcNow.AddHours(hoursUntilDueWarning);

        return await this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value.AddHours(hoursUntilDueWarning) <= warningThreshold)
            .Where(t => !t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value))
            .OrderBy(t => t.UpdatedAt)
            .ToListAsync();
    }

    public Task<List<Ticket>> GetByTagAsync(
        int workspaceId,
        string tag) =>
        Task.FromResult<List<Ticket>>([]);

    public async Task<List<Dictionary<string, object>>> GetBulkDataForExportAsync(
        int workspaceId,
        TicketSearchCriteria criteria)
    {
        var searchResult = await this.SearchAsync(workspaceId, criteria, 0); // System execution
        var statuses = await this.dbContext.TicketStatuses
            .Where(s => s.WorkspaceId == workspaceId)
            .ToListAsync();
        var priorities = await this.dbContext.TicketPriorities
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync();
        var statusMap = statuses.ToDictionary(s => s.Id, s => s.Name);
        var priorityMap = priorities.ToDictionary(p => p.Id, p => p.Name);

        return [.. searchResult.Tickets.Select(t => new Dictionary<string, object>
        {
            { "Id", t.Id },
            { "Subject", t.Subject },
            { "StatusId", t.StatusId ?? 0 },
            { "Status", t.StatusId.HasValue && statusMap.TryGetValue(t.StatusId.Value, out var sn) ? sn : "Unknown" },
            { "PriorityId", t.PriorityId ?? 0 },
            { "Priority", t.PriorityId.HasValue && priorityMap.TryGetValue(t.PriorityId.Value, out var pn) ? pn : "Unknown" },
            { "TypeId", t.TicketTypeId ?? 0 },
            { "CreatedAt", t.CreatedAt },
            { "UpdatedAt", t.UpdatedAt ?? DateTime.MinValue },
            { "AssignedUserId", t.AssignedUserId ?? 0 },
            { "AssignedTeamId", t.AssignedTeamId ?? 0 },
            { "ContactId", t.ContactId ?? 0 }
        })];
    }

    /// <summary>
    /// Applies search criteria as EF Core-compatible IQueryable filters.
    /// All filtering happens at the database level — no in-memory materialization.
    /// </summary>
    private static IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, TicketSearchCriteria criteria)
    {
        if (criteria.AssignedToUserId.HasValue)
        {
            query = query.Where(t => t.AssignedUserId == criteria.AssignedToUserId.Value);
        }

        if (criteria.AssignedToTeamId.HasValue)
        {
            query = query.Where(t => t.AssignedTeamId == criteria.AssignedToTeamId.Value);
        }

        if (criteria.StatusId.HasValue)
        {
            query = query.Where(t => t.StatusId == criteria.StatusId.Value);
        }

        if (criteria.PriorityId.HasValue)
        {
            query = query.Where(t => t.PriorityId == criteria.PriorityId.Value);
        }

        if (criteria.TypeId.HasValue)
        {
            query = query.Where(t => t.TicketTypeId == criteria.TypeId.Value);
        }

        if (criteria.ContactId.HasValue)
        {
            query = query.Where(t => t.ContactId == criteria.ContactId.Value);
        }

        if (criteria.LocationId.HasValue)
        {
            query = query.Where(t => t.LocationId == criteria.LocationId.Value);
        }

        if (criteria.CreatedAfter.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= criteria.CreatedAfter.Value);
        }

        if (criteria.CreatedBefore.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= criteria.CreatedBefore.Value);
        }

        if (criteria.UpdatedAfter.HasValue)
        {
            query = query.Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value >= criteria.UpdatedAfter.Value);
        }

        if (criteria.UpdatedBefore.HasValue)
        {
            query = query.Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value <= criteria.UpdatedBefore.Value);
        }

        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLower();
            query = query.Where(t =>
                (t.Subject != null && t.Subject.ToLower().Contains(term)) ||
                (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        return query;
    }
}
