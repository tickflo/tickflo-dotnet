using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for generating dashboard metrics, statistics, and visualizations.
/// Implements complex aggregation and calculation logic.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly IRolePermissionRepository _rolePerms;

    public DashboardService(
        ITicketRepository ticketRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        IUserRepository userRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ITeamMemberRepository teamMembers,
        IUserWorkspaceRoleRepository uwr,
        IRolePermissionRepository rolePerms)
    {
        _ticketRepo = ticketRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _teamMembers = teamMembers;
        _uwr = uwr;
        _rolePerms = rolePerms;
    }

    public async Task<DashboardTicketStats> GetTicketStatsAsync(
        int workspaceId, 
        int userId, 
        string ticketViewScope, 
        List<int> userTeamIds)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedNames = new HashSet<string>(
            statuses.Where(s => s.IsClosedState).Select(s => s.Name), 
            StringComparer.OrdinalIgnoreCase);
        
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        
        return new DashboardTicketStats
        {
            TotalTickets = visibleTickets.Count,
            OpenTickets = visibleTickets.Count(t => !closedNames.Contains(t.Status)),
            ResolvedTickets = visibleTickets.Count(t => closedNames.Contains(t.Status)),
            ActiveMembers = memberships.Count(m => m.Accepted)
        };
    }

    public async Task<List<ActivityDataPoint>> GetActivitySeriesAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds,
        int daysBack = 30)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedNames = new HashSet<string>(
            statuses.Where(s => s.IsClosedState).Select(s => s.Name), 
            StringComparer.OrdinalIgnoreCase);
        
        var startDate = DateTime.UtcNow.Date.AddDays(-daysBack + 1);
        var dateWindow = Enumerable.Range(0, daysBack).Select(i => startDate.AddDays(i)).ToList();
        
        return dateWindow
            .Select(d => new ActivityDataPoint
            {
                Date = d.ToString("MMM dd"),
                Created = visibleTickets.Count(t => t.CreatedAt.Date == d.Date),
                Closed = visibleTickets.Count(t => 
                    closedNames.Contains(t.Status) && 
                    (t.UpdatedAt ?? t.CreatedAt).Date == d.Date)
            })
            .ToList();
    }

    public async Task<List<TopMember>> GetTopMembersAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds,
        int topN = 5)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedNames = new HashSet<string>(
            statuses.Where(s => s.IsClosedState).Select(s => s.Name), 
            StringComparer.OrdinalIgnoreCase);
        
        var closedAssigned = visibleTickets
            .Where(t => t.AssignedUserId.HasValue && closedNames.Contains(t.Status))
            .GroupBy(t => t.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToList();
        
        var topMembers = new List<TopMember>();
        foreach (var item in closedAssigned)
        {
            var user = await _userRepo.FindByIdAsync(item.UserId);
            topMembers.Add(new TopMember
            {
                UserId = item.UserId,
                Name = user?.Name ?? $"User #{item.UserId}",
                ClosedCount = item.Count
            });
        }
        
        return topMembers;
    }

    public async Task<string> GetAverageResolutionTimeAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var closedNames = new HashSet<string>(
            statuses.Where(s => s.IsClosedState).Select(s => s.Name), 
            StringComparer.OrdinalIgnoreCase);
        
        var closedWithUpdate = visibleTickets
            .Where(t => closedNames.Contains(t.Status) && t.UpdatedAt.HasValue)
            .ToList();
        
        if (closedWithUpdate.Count == 0)
            return "N/A";
        
        var avgTicks = closedWithUpdate.Average(t => (t.UpdatedAt!.Value - t.CreatedAt).Ticks);
        var avgDuration = TimeSpan.FromTicks(Convert.ToInt64(avgTicks));
        
        return FormatDuration(avgDuration);
    }

    public async Task<Dictionary<string, int>> GetPriorityCountsAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        
        var priorities = await _priorityRepo.ListAsync(workspaceId);
        
        var priorityCounts = priorities.ToDictionary(
            p => p.Name,
            p => visibleTickets.Count(t => (t.Priority ?? "Normal") == p.Name)
        );
        
        // Add any tickets with priorities not in the custom list
        foreach (var t in visibleTickets)
        {
            var p = string.IsNullOrWhiteSpace(t.Priority) ? "Normal" : t.Priority;
            if (!priorityCounts.ContainsKey(p))
                priorityCounts[p] = 1;
            else if (!priorities.Any(x => x.Name == p))
                priorityCounts[p]++;
        }
        
        return priorityCounts;
    }

    public List<Ticket> FilterTicketsByAssignment(
        IEnumerable<Ticket> tickets,
        string assignmentFilter,
        int currentUserId)
    {
        IEnumerable<Ticket> filtered = tickets;
        
        switch (assignmentFilter?.ToLowerInvariant())
        {
            case "unassigned":
                filtered = filtered.Where(t => !t.AssignedUserId.HasValue);
                break;
            case "me":
                filtered = filtered.Where(t => t.AssignedUserId == currentUserId);
                break;
            case "others":
                filtered = filtered.Where(t => t.AssignedUserId.HasValue && t.AssignedUserId != currentUserId);
                break;
            // "all" or default: no filter
        }
        
        return filtered.ToList();
    }

    private async Task<List<Ticket>> ApplyTicketScopeFilterAsync(
        IReadOnlyList<Ticket> tickets,
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds)
    {
        IEnumerable<Ticket> visibleTickets = tickets;
        
        var scope = ticketViewScope?.ToLowerInvariant() ?? "all";
        
        if (scope == "mine")
        {
            visibleTickets = visibleTickets.Where(t => t.AssignedUserId == userId);
        }
        else if (scope == "team")
        {
            if (userTeamIds == null || userTeamIds.Count == 0)
            {
                // Load user's teams if not provided
                var myTeams = await _teamMembers.ListTeamsForUserAsync(workspaceId, userId);
                userTeamIds = myTeams.Select(t => t.Id).ToList();
            }
            
            var teamIds = userTeamIds.ToHashSet();
            visibleTickets = visibleTickets.Where(t => 
                t.AssignedTeamId.HasValue && 
                teamIds.Contains(t.AssignedTeamId.Value));
        }
        
        return visibleTickets.ToList();
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{Math.Round(duration.TotalDays, 1)} days";
        if (duration.TotalHours >= 1)
            return $"{Math.Round(duration.TotalHours, 1)} hours";
        if (duration.TotalMinutes >= 1)
            return $"{Math.Round(duration.TotalMinutes, 1)} minutes";
        return $"{Math.Round(duration.TotalSeconds, 1)} seconds";
    }
}
