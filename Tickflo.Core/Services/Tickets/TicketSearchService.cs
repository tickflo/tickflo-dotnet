using Tickflo.Core.Entities;
using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Implementation of ticket search and discovery service.
/// Optimized for complex queries and reporting scenarios.
/// </summary>
public class TicketSearchService : ITicketSearchService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITeamMemberRepository _teamMemberRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;

    public TicketSearchService(
        ITicketRepository ticketRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ITeamMemberRepository teamMemberRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo)
    {
        _ticketRepo = ticketRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _teamMemberRepo = teamMemberRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
    }

    public async Task<TicketSearchResult> SearchAsync(
        int workspaceId,
        TicketSearchCriteria criteria,
        int requestingUserId)
    {
        // Validation: User has access to workspace
        var userAccess = await _userWorkspaceRepo.FindAsync(requestingUserId, workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        // Get all tickets in workspace
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();

        // Apply filters
        var filtered = ApplyFilters(allTickets, criteria);

        // Apply pagination
        var total = filtered.Count;
        var skip = (criteria.PageNumber - 1) * criteria.PageSize;
        var paginated = filtered
            .Skip(skip)
            .Take(criteria.PageSize)
            .ToList();

        return new TicketSearchResult
        {
            Tickets = paginated,
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
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();

        var myTickets = allTickets
            .Where(t => t.AssignedUserId == userId || 
                       (t.AssignedTeamId.HasValue && 
                        IsUserInTeam(userId, t.AssignedTeamId.Value).GetAwaiter().GetResult()))
            .ToList();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var statusId = (await _statusRepo.FindByNameAsync(workspaceId, statusFilter))?.Id;
            if (statusId.HasValue)
                myTickets = myTickets.Where(t => t.StatusId == statusId.Value).ToList();
        }

        return myTickets;
    }

    public async Task<List<Ticket>> GetActiveTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();

        var active = allTickets
            .Where(t => !t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value))
            .ToList();

        if (limitToTeamId.HasValue)
        {
            active = active.Where(t => t.AssignedTeamId == limitToTeamId).ToList();
        }

        return active;
    }

    public async Task<List<Ticket>> GetRecentlyUpdatedAsync(
        int workspaceId,
        int limitToLastDays = 7,
        int take = 20)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-limitToLastDays);
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();

        var recent = allTickets
            .Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value >= cutoffDate)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(take)
            .ToList();

        return recent;
    }

    public async Task<List<Ticket>> GetHighPriorityTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();
        var priorities = await _priorityRepo.ListAsync(workspaceId);
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var highPriorityIds = priorities.Where(p => p.Name == "Critical" || p.Name == "High").Select(p => p.Id).ToHashSet();
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();

        var highPriority = allTickets
            .Where(t => (t.PriorityId.HasValue && highPriorityIds.Contains(t.PriorityId.Value)) &&
                       (!t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value)))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        if (limitToTeamId.HasValue)
        {
            highPriority = highPriority.Where(t => t.AssignedTeamId == limitToTeamId).ToList();
        }

        return highPriority;
    }

    public async Task<List<Ticket>> GetContactTicketsAsync(
        int workspaceId,
        int contactId)
    {
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();

        return allTickets
            .Where(t => t.ContactId == contactId)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }

    public async Task<List<Ticket>> GetUnassignedTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();

        var unassigned = allTickets
            .Where(t => t.AssignedUserId == null && t.AssignedTeamId == null &&
                       (!t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value)))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        if (limitToTeamId.HasValue)
        {
            unassigned = unassigned.Where(t => t.AssignedTeamId == limitToTeamId).ToList();
        }

        return unassigned;
    }

    public async Task<List<Ticket>> GetSLAAtRiskAsync(
        int workspaceId,
        int hoursUntilDueWarning = 24)
    {
        var allTickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();
        var warningThreshold = DateTime.UtcNow.AddHours(hoursUntilDueWarning);

        // Tickets don't have DueDate; check UpdatedAt + hours
        return allTickets
            .Where(t => t.UpdatedAt.HasValue && 
                       t.UpdatedAt.Value.AddHours(hoursUntilDueWarning) <= warningThreshold &&
                       (!t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value)))
            .OrderBy(t => t.UpdatedAt)
            .ToList();
    }

    public async Task<List<Ticket>> GetByTagAsync(
        int workspaceId,
        string tag)
    {
        // Tags not implemented in current Ticket schema
        return new List<Ticket>();
    }

    public async Task<List<Dictionary<string, object>>> GetBulkDataForExportAsync(
        int workspaceId,
        TicketSearchCriteria criteria)
    {
        var searchResult = await SearchAsync(workspaceId, criteria, 0); // System execution
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var priorities = await _priorityRepo.ListAsync(workspaceId);
        var statusMap = statuses.ToDictionary(s => s.Id, s => s.Name);
        var priorityMap = priorities.ToDictionary(p => p.Id, p => p.Name);

        return searchResult.Tickets.Select(t => new Dictionary<string, object>
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
        }).ToList();
    }

    private List<Ticket> ApplyFilters(List<Ticket> tickets, TicketSearchCriteria criteria)
    {
        var result = tickets;

        if (criteria.AssignedToUserId.HasValue)
            result = result.Where(t => t.AssignedUserId == criteria.AssignedToUserId.Value).ToList();

        if (criteria.AssignedToTeamId.HasValue)
            result = result.Where(t => t.AssignedTeamId == criteria.AssignedToTeamId.Value).ToList();

        if (criteria.StatusId.HasValue)
            result = result.Where(t => t.StatusId == criteria.StatusId.Value).ToList();

        if (criteria.PriorityId.HasValue)
            result = result.Where(t => t.PriorityId == criteria.PriorityId.Value).ToList();

        if (criteria.TypeId.HasValue)
            result = result.Where(t => t.TicketTypeId == criteria.TypeId.Value).ToList();

        if (criteria.ContactId.HasValue)
            result = result.Where(t => t.ContactId == criteria.ContactId.Value).ToList();

        if (criteria.LocationId.HasValue)
            result = result.Where(t => t.LocationId == criteria.LocationId.Value).ToList();

        if (criteria.CreatedAfter.HasValue)
            result = result.Where(t => t.CreatedAt >= criteria.CreatedAfter.Value).ToList();

        if (criteria.CreatedBefore.HasValue)
            result = result.Where(t => t.CreatedAt <= criteria.CreatedBefore.Value).ToList();

        if (criteria.UpdatedAfter.HasValue)
            result = result.Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value >= criteria.UpdatedAfter.Value).ToList();

        if (criteria.UpdatedBefore.HasValue)
            result = result.Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value <= criteria.UpdatedBefore.Value).ToList();

        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLowerInvariant();
            result = result.Where(t => 
                (t.Subject?.ToLowerInvariant().Contains(term) ?? false) ||
                (t.Description?.ToLowerInvariant().Contains(term) ?? false)
            ).ToList();
        }

        return result;
    }

    private async Task<bool> IsUserInTeam(int userId, int teamId)
    {
        var members = await _teamMemberRepo.ListMembersAsync(teamId);
        return members.Any(m => m.Id == userId);
    }
}
