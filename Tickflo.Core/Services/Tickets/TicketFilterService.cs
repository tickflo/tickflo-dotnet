using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Service for filtering and searching tickets based on multiple criteria.
/// </summary>
public class TicketFilterService : ITicketFilterService
{
    public List<Ticket> ApplyFilters(IEnumerable<Ticket> tickets, TicketFilterCriteria filter)
    {
        IEnumerable<Ticket> filtered = tickets;

        // Text search filter
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var query = filter.Query.Trim().ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Subject ?? string.Empty).ToLowerInvariant().Contains(query) ||
                (t.Description ?? string.Empty).ToLowerInvariant().Contains(query));
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

        return filtered.ToList();
    }

    public List<Ticket> ApplyScopeFilter(
        IEnumerable<Ticket> tickets,
        int userId,
        string scope,
        List<int> userTeamIds)
    {
        IEnumerable<Ticket> filtered = tickets;

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
            // "all" - no filter
        }

        return filtered.ToList();
    }

    public int CountMyTickets(IEnumerable<Ticket> tickets, int userId)
    {
        return tickets.Count(t => t.AssignedUserId == userId);
    }
}


