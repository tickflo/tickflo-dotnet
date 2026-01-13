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

        // Status filter
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var status = filter.Status.Trim();
            filtered = filtered.Where(t =>
                string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        // Priority filter
        if (!string.IsNullOrWhiteSpace(filter.Priority))
        {
            var priority = filter.Priority.Trim();
            filtered = filtered.Where(t =>
                string.Equals(t.Priority ?? "Normal", priority, StringComparison.OrdinalIgnoreCase));
        }

        // Type filter
        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            var type = filter.Type.Trim();
            filtered = filtered.Where(t =>
                string.Equals(t.Type, type, StringComparison.OrdinalIgnoreCase));
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


