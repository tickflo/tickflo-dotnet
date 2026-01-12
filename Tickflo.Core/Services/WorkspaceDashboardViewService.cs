using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceDashboardViewService : IWorkspaceDashboardViewService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly IUserRepository _userRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IDashboardService _dashboardService;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;

    public WorkspaceDashboardViewService(
        ITicketRepository ticketRepo,
        ITicketStatusRepository statusRepo,
        ITicketTypeRepository typeRepo,
        ITicketPriorityRepository priorityRepo,
        IUserRepository userRepo,
        ITeamRepository teamRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IDashboardService dashboardService,
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms)
    {
        _ticketRepo = ticketRepo;
        _statusRepo = statusRepo;
        _typeRepo = typeRepo;
        _priorityRepo = priorityRepo;
        _userRepo = userRepo;
        _teamRepo = teamRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _dashboardService = dashboardService;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
    }

    public async Task<WorkspaceDashboardView> BuildAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        int rangeDays,
        string assignmentFilter)
    {
        var stats = await _dashboardService.GetTicketStatsAsync(workspaceId, userId, scope, teamIds.ToList());

        var statusList = (await _statusRepo.ListAsync(workspaceId)).ToList();
        var typeList = (await _typeRepo.ListAsync(workspaceId)).ToList();
        var priorityList = (await _priorityRepo.ListAsync(workspaceId)).ToList();
        var priorityCounts = await _dashboardService.GetPriorityCountsAsync(workspaceId, userId, scope, teamIds.ToList());

        var (primaryColor, primaryIsHex, successColor, successIsHex) = ResolveColors(statusList);

        var acceptedUserIds = (await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId))
            .Where(m => m.Accepted)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();

        var members = new List<User>();
        foreach (var uid in acceptedUserIds)
        {
            var user = await _userRepo.FindByIdAsync(uid);
            if (user != null) members.Add(user);
        }

        var teams = await _teamRepo.ListForWorkspaceAsync(workspaceId);

        var activityData = await _dashboardService.GetActivitySeriesAsync(workspaceId, userId, scope, teamIds.ToList(), rangeDays);
        var activitySeries = activityData.Select(a => new DashboardActivityPoint(a.Date, a.Created, a.Closed)).ToList();

        var topMembers = await _dashboardService.GetTopMembersAsync(workspaceId, userId, scope, teamIds.ToList(), topN: 5);
        var topMemberStats = topMembers.Select(m => new DashboardMemberStat(m.UserId, m.Name, m.ClosedCount)).ToList();

        var avgResolutionLabel = await _dashboardService.GetAverageResolutionTimeAsync(workspaceId, userId, scope, teamIds.ToList());

        var recentTickets = await GetRecentTicketsAsync(workspaceId, userId, scope, teamIds, assignmentFilter, statusList, typeList);

        // Compute permissions
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        bool canViewDashboard = false;
        bool canViewTickets = false;
        string ticketViewScope = scope;

        if (isAdmin)
        {
            canViewDashboard = true;
            canViewTickets = true;
            ticketViewScope = "all";
        }
        else
        {
            var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (eff.TryGetValue("dashboard", out var dp)) canViewDashboard = dp.CanView;
            if (eff.TryGetValue("tickets", out var tp)) canViewTickets = tp.CanView;
            ticketViewScope = await _rolePerms.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
        }

        return new WorkspaceDashboardView(
            stats.TotalTickets,
            stats.OpenTickets,
            stats.ResolvedTickets,
            stats.ActiveMembers,
            statusList,
            typeList,
            priorityList,
            priorityCounts,
            primaryColor,
            primaryIsHex,
            successColor,
            successIsHex,
            members,
            teams,
            activitySeries,
            topMemberStats,
            avgResolutionLabel,
            recentTickets,
            canViewDashboard,
            canViewTickets,
            ticketViewScope);
    }

    private async Task<List<DashboardTicketListItem>> GetRecentTicketsAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        string assignmentFilter,
        IReadOnlyList<TicketStatus> statusList,
        IReadOnlyList<TicketType> typeList)
    {
        var scopedTickets = await ApplyTicketScopeAsync(workspaceId, userId, scope, teamIds);
        var allTickets = _dashboardService.FilterTicketsByAssignment(scopedTickets, assignmentFilter, userId);

        var statusColor = statusList.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Color, StringComparer.OrdinalIgnoreCase);
        var typeColor = typeList.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Color, StringComparer.OrdinalIgnoreCase);

        var recent = allTickets
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Take(8)
            .ToList();

        var assigneeIds = recent.Where(t => t.AssignedUserId.HasValue)
            .Select(t => t.AssignedUserId!.Value)
            .Distinct()
            .ToList();
        var assigneeNames = new Dictionary<int, string>();
        foreach (var uid in assigneeIds)
        {
            var u = await _userRepo.FindByIdAsync(uid);
            if (u != null) assigneeNames[uid] = u.Name;
        }

        return recent.Select(t => new DashboardTicketListItem(
            t.Id,
            t.Subject,
            t.Type,
            t.Status,
            statusColor.TryGetValue(t.Status, out var c) ? c : "neutral",
            typeColor.TryGetValue(t.Type, out var tc) ? tc : "neutral",
            t.AssignedUserId,
            t.AssignedUserId.HasValue && assigneeNames.TryGetValue(t.AssignedUserId.Value, out var assigneeName) ? assigneeName : null,
            t.UpdatedAt ?? t.CreatedAt)).ToList();
    }

    private async Task<List<Ticket>> ApplyTicketScopeAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds)
    {
        var tickets = await _ticketRepo.ListAsync(workspaceId);

        if (scope == "mine")
        {
            return tickets.Where(t => t.AssignedUserId == userId).ToList();
        }
        else if (scope == "team")
        {
            var teamIdSet = teamIds.ToHashSet();
            return tickets.Where(t => t.AssignedTeamId.HasValue && teamIdSet.Contains(t.AssignedTeamId.Value)).ToList();
        }

        return tickets.ToList();
    }

    private static (string PrimaryColor, bool PrimaryIsHex, string SuccessColor, bool SuccessIsHex) ResolveColors(IReadOnlyList<TicketStatus> statusList)
    {
        var openStatus = statusList.FirstOrDefault(s => !s.IsClosedState);
        var closedStatus = statusList.FirstOrDefault(s => s.IsClosedState);

        string primaryColor = "primary";
        bool primaryIsHex = false;
        string successColor = "success";
        bool successIsHex = false;

        if (openStatus != null && !string.IsNullOrWhiteSpace(openStatus.Color))
        {
            primaryColor = openStatus.Color;
            primaryIsHex = openStatus.Color.StartsWith("#");
        }

        if (closedStatus != null && !string.IsNullOrWhiteSpace(closedStatus.Color))
        {
            successColor = closedStatus.Color;
            successIsHex = closedStatus.Color.StartsWith("#");
        }

        return (primaryColor, primaryIsHex, successColor, successIsHex);
    }
}
