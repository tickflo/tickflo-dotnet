using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Common;

/// <summary>
/// Service for generating dashboard metrics, statistics, and visualizations.
/// Implements complex aggregation and calculation logic.
/// </summary>
public class DashboardService : IDashboardService
{
    #region Constants
    private const string DateFormat = "MMM dd";
    private const string AllScope = "all";
    private const string MineScope = "mine";
    private const string TeamScope = "team";
    private const string UnassignedFilter = "unassigned";
    private const string MeFilter = "me";
    private const string OthersFilter = "others";
    private const string NoDataAvailable = "N/A";
    private const string UserNameFormat = "User #{0}";
    private const int DefaultTopMembersCount = 5;
    private const int DefaultActivityDaysBack = 30;
    #endregion

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
        
        var closedIds = await GetClosedStatusIdsAsync(workspaceId);
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        
        return new DashboardTicketStats
        {
            TotalTickets = visibleTickets.Count,
            OpenTickets = visibleTickets.Count(t => !IsTicketClosed(t, closedIds)),
            ResolvedTickets = visibleTickets.Count(t => IsTicketClosed(t, closedIds)),
            ActiveMembers = memberships.Count(m => m.Accepted)
        };
    }

    public async Task<List<ActivityDataPoint>> GetActivitySeriesAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds,
        int daysBack = DefaultActivityDaysBack)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        var closedIds = await GetClosedStatusIdsAsync(workspaceId);
        
        var dateWindow = GenerateDateWindow(daysBack);
        
        return dateWindow
            .Select(d => new ActivityDataPoint
            {
                Date = d.ToString(DateFormat),
                Created = visibleTickets.Count(t => t.CreatedAt.Date == d.Date),
                Closed = visibleTickets.Count(t => 
                    IsTicketClosed(t, closedIds) && 
                    (t.UpdatedAt ?? t.CreatedAt).Date == d.Date)
            })
            .ToList();
    }

    public async Task<List<TopMember>> GetTopMembersAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds,
        int topN = DefaultTopMembersCount)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        var closedIds = await GetClosedStatusIdsAsync(workspaceId);
        
        var closedAssigned = visibleTickets
            .Where(t => t.AssignedUserId.HasValue && IsTicketClosed(t, closedIds))
            .GroupBy(t => t.AssignedUserId!.Value)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .ToList();
        
        return await BuildTopMembersListAsync(closedAssigned);
    }

    public async Task<string> GetAverageResolutionTimeAsync(
        int workspaceId,
        int userId,
        string ticketViewScope,
        List<int> userTeamIds)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);
        var visibleTickets = await ApplyTicketScopeFilterAsync(tickets, workspaceId, userId, ticketViewScope, userTeamIds);
        var closedIds = await GetClosedStatusIdsAsync(workspaceId);
        
        var closedWithUpdate = visibleTickets
            .Where(t => IsTicketClosed(t, closedIds) && t.UpdatedAt.HasValue)
            .ToList();
        
        if (closedWithUpdate.Count == 0)
            return NoDataAvailable;
        
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

        // Count by PriorityId when available, otherwise by name
        var byId = priorities.ToDictionary(p => p.Id, p => 0);
        foreach (var t in visibleTickets)
        {
            if (t.PriorityId.HasValue && byId.ContainsKey(t.PriorityId.Value))
                byId[t.PriorityId.Value]++;
        }

        // Build result keyed by name
        var priorityCounts = priorities.ToDictionary(
            p => p.Name,
            p => byId.TryGetValue(p.Id, out var cnt) ? cnt : 0
        );

        // Include any tickets without PriorityId by skipping them
        // (All tickets should have PriorityId now, but handle null case for safety)
        foreach (var t in visibleTickets.Where(x => !x.PriorityId.HasValue))
        {
            // Skip tickets without PriorityId - they should not exist in ID-only model
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
            case UnassignedFilter:
                filtered = filtered.Where(t => !t.AssignedUserId.HasValue);
                break;
            case MeFilter:
                filtered = filtered.Where(t => t.AssignedUserId == currentUserId);
                break;
            case OthersFilter:
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
        
        var scope = ticketViewScope?.ToLowerInvariant() ?? AllScope;
        
        if (scope == MineScope)
        {
            visibleTickets = visibleTickets.Where(t => t.AssignedUserId == userId);
        }
        else if (scope == TeamScope)
        {
            var teamIds = await GetUserTeamIdsAsync(workspaceId, userId, userTeamIds);
            visibleTickets = visibleTickets.Where(t => 
                t.AssignedTeamId.HasValue && 
                teamIds.Contains(t.AssignedTeamId.Value));
        }
        
        return visibleTickets.ToList();
    }

    private async Task<HashSet<int>> GetClosedStatusIdsAsync(int workspaceId)
    {
        var statuses = await _statusRepo.ListAsync(workspaceId);
        return new HashSet<int>(statuses.Where(s => s.IsClosedState).Select(s => s.Id));
    }

    private bool IsTicketClosed(Ticket ticket, HashSet<int> closedStatusIds)
    {
        return ticket.StatusId.HasValue && closedStatusIds.Contains(ticket.StatusId.Value);
    }

    private List<DateTime> GenerateDateWindow(int daysBack)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-daysBack + 1);
        return Enumerable.Range(0, daysBack).Select(i => startDate.AddDays(i)).ToList();
    }

    private async Task<List<TopMember>> BuildTopMembersListAsync(IEnumerable<dynamic> closedAssigned)
    {
        var topMembers = new List<TopMember>();
        foreach (var item in closedAssigned)
        {
            var user = await _userRepo.FindByIdAsync(item.UserId);
            topMembers.Add(new TopMember
            {
                UserId = item.UserId,
                Name = user?.Name ?? string.Format(UserNameFormat, item.UserId),
                ClosedCount = item.Count
            });
        }
        return topMembers;
    }

    private async Task<HashSet<int>> GetUserTeamIdsAsync(int workspaceId, int userId, List<int> userTeamIds)
    {
        if (userTeamIds == null || userTeamIds.Count == 0)
        {
            var myTeams = await _teamMembers.ListTeamsForUserAsync(workspaceId, userId);
            userTeamIds = myTeams.Select(t => t.Id).ToList();
        }
        return userTeamIds.ToHashSet();
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


