namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of ticket search and discovery service.
/// Optimized for complex queries and reporting scenarios.
/// </summary>
public class TicketSearchService(
    ITicketRepository ticketRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    ITeamMemberRepository teamMemberRepo,
    ITicketStatusRepository statusRepository,
    ITicketPriorityRepository priorityRepository) : ITicketSearchService
{
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMemberRepo;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;

    public async Task<TicketSearchResult> SearchAsync(
        int workspaceId,
        TicketSearchCriteria criteria,
        int requestingUserId)
    {
        // Validation: User has access to workspace
        var userAccess = await this.userWorkspaceRepository.FindAsync(requestingUserId, workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        // Get all tickets in workspace
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();

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
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();

        var myTickets = allTickets
            .Where(t => t.AssignedUserId == userId ||
                       (t.AssignedTeamId.HasValue &&
                        this.IsUserInTeam(userId, t.AssignedTeamId.Value).GetAwaiter().GetResult()))
            .ToList();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var statusId = (await this.statusRepository.FindByNameAsync(workspaceId, statusFilter))?.Id;
            if (statusId.HasValue)
            {
                myTickets = [.. myTickets.Where(t => t.StatusId == statusId.Value)];
            }
        }

        return myTickets;
    }

    public async Task<List<Ticket>> GetActiveTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();
        var statuses = await this.statusRepository.ListAsync(workspaceId);
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();

        var active = allTickets
            .Where(t => !t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value))
            .ToList();

        if (limitToTeamId.HasValue)
        {
            active = [.. active.Where(t => t.AssignedTeamId == limitToTeamId)];
        }

        return active;
    }

    public async Task<List<Ticket>> GetRecentlyUpdatedAsync(
        int workspaceId,
        int limitToLastDays = 7,
        int take = 20)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-limitToLastDays);
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();

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
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();
        var priorities = await this.priorityRepository.ListAsync(workspaceId);
        var statuses = await this.statusRepository.ListAsync(workspaceId);
        var highPriorityIds = priorities.Where(p => p.Name is "Critical" or "High").Select(p => p.Id).ToHashSet();
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();

        var highPriority = allTickets
            .Where(t => t.PriorityId.HasValue && highPriorityIds.Contains(t.PriorityId.Value) &&
                       (!t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value)))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        if (limitToTeamId.HasValue)
        {
            highPriority = [.. highPriority.Where(t => t.AssignedTeamId == limitToTeamId)];
        }

        return highPriority;
    }

    public async Task<List<Ticket>> GetContactTicketsAsync(
        int workspaceId,
        int contactId)
    {
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();

        return [.. allTickets
            .Where(t => t.ContactId == contactId)
            .OrderByDescending(t => t.CreatedAt)];
    }

    public async Task<List<Ticket>> GetUnassignedTicketsAsync(
        int workspaceId,
        int? limitToTeamId = null)
    {
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();
        var statuses = await this.statusRepository.ListAsync(workspaceId);
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();

        var unassigned = allTickets
            .Where(t => t.AssignedUserId == null && t.AssignedTeamId == null &&
                       (!t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value)))
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        if (limitToTeamId.HasValue)
        {
            unassigned = [.. unassigned.Where(t => t.AssignedTeamId == limitToTeamId)];
        }

        return unassigned;
    }

    public async Task<List<Ticket>> GetSLAAtRiskAsync(
        int workspaceId,
        int hoursUntilDueWarning = 24)
    {
        var allTickets = (await this.ticketRepository.ListAsync(workspaceId)).ToList();
        var statuses = await this.statusRepository.ListAsync(workspaceId);
        var closedIds = statuses.Where(s => s.IsClosedState).Select(s => s.Id).ToHashSet();
        var warningThreshold = DateTime.UtcNow.AddHours(hoursUntilDueWarning);

        // Tickets don't have DueDate; check UpdatedAt + hours
        return [.. allTickets
            .Where(t => t.UpdatedAt.HasValue &&
                       t.UpdatedAt.Value.AddHours(hoursUntilDueWarning) <= warningThreshold &&
                       (!t.StatusId.HasValue || !closedIds.Contains(t.StatusId.Value)))
            .OrderBy(t => t.UpdatedAt)];
    }

    public async Task<List<Ticket>> GetByTagAsync(
        int workspaceId,
        string tag) =>
        // Tags not implemented in current Ticket schema
        [];

    public async Task<List<Dictionary<string, object>>> GetBulkDataForExportAsync(
        int workspaceId,
        TicketSearchCriteria criteria)
    {
        var searchResult = await this.SearchAsync(workspaceId, criteria, 0); // System execution
        var statuses = await this.statusRepository.ListAsync(workspaceId);
        var priorities = await this.priorityRepository.ListAsync(workspaceId);
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

    private static List<Ticket> ApplyFilters(List<Ticket> tickets, TicketSearchCriteria criteria)
    {
        var result = tickets;

        if (criteria.AssignedToUserId.HasValue)
        {
            result = [.. result.Where(t => t.AssignedUserId == criteria.AssignedToUserId.Value)];
        }

        if (criteria.AssignedToTeamId.HasValue)
        {
            result = [.. result.Where(t => t.AssignedTeamId == criteria.AssignedToTeamId.Value)];
        }

        if (criteria.StatusId.HasValue)
        {
            result = [.. result.Where(t => t.StatusId == criteria.StatusId.Value)];
        }

        if (criteria.PriorityId.HasValue)
        {
            result = [.. result.Where(t => t.PriorityId == criteria.PriorityId.Value)];
        }

        if (criteria.TypeId.HasValue)
        {
            result = [.. result.Where(t => t.TicketTypeId == criteria.TypeId.Value)];
        }

        if (criteria.ContactId.HasValue)
        {
            result = [.. result.Where(t => t.ContactId == criteria.ContactId.Value)];
        }

        if (criteria.LocationId.HasValue)
        {
            result = [.. result.Where(t => t.LocationId == criteria.LocationId.Value)];
        }

        if (criteria.CreatedAfter.HasValue)
        {
            result = [.. result.Where(t => t.CreatedAt >= criteria.CreatedAfter.Value)];
        }

        if (criteria.CreatedBefore.HasValue)
        {
            result = [.. result.Where(t => t.CreatedAt <= criteria.CreatedBefore.Value)];
        }

        if (criteria.UpdatedAfter.HasValue)
        {
            result = [.. result.Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value >= criteria.UpdatedAfter.Value)];
        }

        if (criteria.UpdatedBefore.HasValue)
        {
            result = [.. result.Where(t => t.UpdatedAt.HasValue && t.UpdatedAt.Value <= criteria.UpdatedBefore.Value)];
        }

        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLowerInvariant();
            result = [.. result.Where(t =>
                (t.Subject?.ToLowerInvariant().Contains(term, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                (t.Description?.ToLowerInvariant().Contains(term, StringComparison.InvariantCultureIgnoreCase) ?? false)
            )];
        }

        return result;
    }

    private async Task<bool> IsUserInTeam(int userId, int teamId)
    {
        var members = await this.teamMemberRepository.ListMembersAsync(teamId);
        return members.Any(m => m.Id == userId);
    }
}
