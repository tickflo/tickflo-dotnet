namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Common;

public class WorkspaceDashboardViewService(
    ITicketRepository ticketRepository,
    ITicketStatusRepository statusRepository,
    ITicketTypeRepository ticketTypeRepository,
    ITicketPriorityRepository priorityRepository,
    IUserRepository userRepository,
    ITeamRepository teamRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IDashboardService dashboardService,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceDashboardViewService
{
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketTypeRepository ticketTypeRepository = ticketTypeRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IDashboardService dashboardService = dashboardService;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<WorkspaceDashboardView> BuildAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        int rangeDays,
        string assignmentFilter)
    {
        var stats = await this.dashboardService.GetTicketStatsAsync(workspaceId, userId, scope, [.. teamIds]);

        var statusList = (await this.statusRepository.ListAsync(workspaceId)).ToList();
        var typeList = (await this.ticketTypeRepository.ListAsync(workspaceId)).ToList();
        var priorityList = (await this.priorityRepository.ListAsync(workspaceId)).ToList();
        var priorityCounts = await this.dashboardService.GetPriorityCountsAsync(workspaceId, userId, scope, [.. teamIds]);

        var (primaryColor, primaryIsHex, successColor, successIsHex) = ResolveColors(statusList);

        var acceptedUserIds = (await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId))
            .Where(m => m.Accepted)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();

        var members = new List<User>();
        foreach (var uid in acceptedUserIds)
        {
            var user = await this.userRepository.FindByIdAsync(uid);
            if (user != null)
            {
                members.Add(user);
            }
        }

        var teams = await this.teamRepository.ListForWorkspaceAsync(workspaceId);

        var activityData = await this.dashboardService.GetActivitySeriesAsync(workspaceId, userId, scope, [.. teamIds], rangeDays);
        var activitySeries = activityData.Select(a => new DashboardActivityPoint(a.Date, a.Created, a.Closed)).ToList();

        var topMembers = await this.dashboardService.GetTopMembersAsync(workspaceId, userId, scope, [.. teamIds], topN: 5);
        var topMemberStats = topMembers.Select(m => new DashboardMemberStat(m.UserId, m.Name, m.ClosedCount)).ToList();

        var avgResolutionLabel = await this.dashboardService.GetAverageResolutionTimeAsync(workspaceId, userId, scope, [.. teamIds]);

        var recentTickets = await this.GetRecentTicketsAsync(workspaceId, userId, scope, teamIds, assignmentFilter, statusList, typeList);

        // Compute permissions
        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var canViewDashboard = false;
        var canViewTickets = false;
        var ticketViewScope = scope;

        if (isAdmin)
        {
            canViewDashboard = true;
            canViewTickets = true;
            ticketViewScope = "all";
        }
        else
        {
            var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (eff.TryGetValue("dashboard", out var dp))
            {
                canViewDashboard = dp.CanView;
            }

            if (eff.TryGetValue("tickets", out var tp))
            {
                canViewTickets = tp.CanView;
            }

            ticketViewScope = await this.rolePermissionRepository.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
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
        var scopedTickets = await this.ApplyTicketScopeAsync(workspaceId, userId, scope, teamIds);
        var allTickets = this.dashboardService.FilterTicketsByAssignment(scopedTickets, assignmentFilter, userId);

        var statusColor = statusList.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Color, StringComparer.OrdinalIgnoreCase);
        var statusColorById = statusList.ToDictionary(s => s.Id, s => s.Color);
        var statusNameById = statusList.ToDictionary(s => s.Id, s => s.Name);
        var typeColor = typeList.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Color, StringComparer.OrdinalIgnoreCase);
        var typeColorById = typeList.ToDictionary(t => t.Id, t => t.Color);
        var typeNameById = typeList.ToDictionary(t => t.Id, t => t.Name);

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
            var user = await this.userRepository.FindByIdAsync(uid);
            if (user != null)
            {
                assigneeNames[uid] = user.Name;
            }
        }

        return [.. recent.Select(t => new DashboardTicketListItem(
            t.Id,
            t.Subject,
            t.TicketTypeId.HasValue && typeNameById.TryGetValue(t.TicketTypeId.Value, out var typeName)
                ? typeName
                : "Unknown",
            t.StatusId.HasValue && statusNameById.TryGetValue(t.StatusId.Value, out var statusName)
                ? statusName
                : "Unknown",
            t.StatusId.HasValue && statusColorById.TryGetValue(t.StatusId.Value, out var cById)
                ? cById
                : "neutral",
            t.TicketTypeId.HasValue && typeColorById.TryGetValue(t.TicketTypeId.Value, out var tcById)
                ? tcById
                : "neutral",
            t.AssignedUserId,
            t.AssignedUserId.HasValue && assigneeNames.TryGetValue(t.AssignedUserId.Value, out var assigneeName) ? assigneeName : null,
            t.UpdatedAt ?? t.CreatedAt))];
    }

    private async Task<List<Ticket>> ApplyTicketScopeAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds)
    {
        var tickets = await this.ticketRepository.ListAsync(workspaceId);

        if (scope == "mine")
        {
            return [.. tickets.Where(t => t.AssignedUserId == userId)];
        }
        else if (scope == "team")
        {
            var teamIdSet = teamIds.ToHashSet();
            return [.. tickets.Where(t => t.AssignedTeamId.HasValue && teamIdSet.Contains(t.AssignedTeamId.Value))];
        }

        return [.. tickets];
    }

    private static (string PrimaryColor, bool PrimaryIsHex, string SuccessColor, bool SuccessIsHex) ResolveColors(IReadOnlyList<TicketStatus> statusList)
    {
        var openStatus = statusList.FirstOrDefault(s => !s.IsClosedState);
        var closedStatus = statusList.FirstOrDefault(s => s.IsClosedState);

        var primaryColor = "primary";
        var primaryIsHex = false;
        var successColor = "success";
        var successIsHex = false;

        if (openStatus != null && !string.IsNullOrWhiteSpace(openStatus.Color))
        {
            primaryColor = openStatus.Color;
            primaryIsHex = openStatus.Color.StartsWith('#');
        }

        if (closedStatus != null && !string.IsNullOrWhiteSpace(closedStatus.Color))
        {
            successColor = closedStatus.Color;
            successIsHex = closedStatus.Color.StartsWith('#');
        }

        return (primaryColor, primaryIsHex, successColor, successIsHex);
    }
}



