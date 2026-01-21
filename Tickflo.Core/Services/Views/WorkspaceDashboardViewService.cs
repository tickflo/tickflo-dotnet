namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Common;

public class WorkspaceDashboardViewService(
    ITicketRepository ticketRepo,
    ITicketStatusRepository statusRepo,
    ITicketTypeRepository typeRepo,
    ITicketPriorityRepository priorityRepo,
    IUserRepository userRepo,
    ITeamRepository teamRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    IDashboardService dashboardService,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms) : IWorkspaceDashboardViewService
{
    private readonly ITicketRepository _ticketRepo = ticketRepo;
    private readonly ITicketStatusRepository _statusRepo = statusRepo;
    private readonly ITicketTypeRepository _typeRepo = typeRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ITeamRepository _teamRepo = teamRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;

    public async Task<WorkspaceDashboardView> BuildAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        int rangeDays,
        string assignmentFilter)
    {
        var stats = await this._dashboardService.GetTicketStatsAsync(workspaceId, userId, scope, [.. teamIds]);

        var statusList = (await this._statusRepo.ListAsync(workspaceId)).ToList();
        var typeList = (await this._typeRepo.ListAsync(workspaceId)).ToList();
        var priorityList = (await this._priorityRepo.ListAsync(workspaceId)).ToList();
        var priorityCounts = await this._dashboardService.GetPriorityCountsAsync(workspaceId, userId, scope, [.. teamIds]);

        var (primaryColor, primaryIsHex, successColor, successIsHex) = ResolveColors(statusList);

        var acceptedUserIds = (await this._userWorkspaceRepo.FindForWorkspaceAsync(workspaceId))
            .Where(m => m.Accepted)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();

        var members = new List<User>();
        foreach (var uid in acceptedUserIds)
        {
            var user = await this._userRepo.FindByIdAsync(uid);
            if (user != null)
            {
                members.Add(user);
            }
        }

        var teams = await this._teamRepo.ListForWorkspaceAsync(workspaceId);

        var activityData = await this._dashboardService.GetActivitySeriesAsync(workspaceId, userId, scope, [.. teamIds], rangeDays);
        var activitySeries = activityData.Select(a => new DashboardActivityPoint(a.Date, a.Created, a.Closed)).ToList();

        var topMembers = await this._dashboardService.GetTopMembersAsync(workspaceId, userId, scope, [.. teamIds], topN: 5);
        var topMemberStats = topMembers.Select(m => new DashboardMemberStat(m.UserId, m.Name, m.ClosedCount)).ToList();

        var avgResolutionLabel = await this._dashboardService.GetAverageResolutionTimeAsync(workspaceId, userId, scope, [.. teamIds]);

        var recentTickets = await this.GetRecentTicketsAsync(workspaceId, userId, scope, teamIds, assignmentFilter, statusList, typeList);

        // Compute permissions
        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
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
            var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (eff.TryGetValue("dashboard", out var dp))
            {
                canViewDashboard = dp.CanView;
            }

            if (eff.TryGetValue("tickets", out var tp))
            {
                canViewTickets = tp.CanView;
            }

            ticketViewScope = await this._rolePerms.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
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
        var allTickets = this._dashboardService.FilterTicketsByAssignment(scopedTickets, assignmentFilter, userId);

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
            var u = await this._userRepo.FindByIdAsync(uid);
            if (u != null)
            {
                assigneeNames[uid] = u.Name;
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
        var tickets = await this._ticketRepo.ListAsync(workspaceId);

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



