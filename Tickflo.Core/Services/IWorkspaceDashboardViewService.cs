using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public record DashboardActivityPoint(string Label, int Created, int Closed);
public record DashboardMemberStat(int UserId, string Name, int ResolvedCount);
public record DashboardTicketListItem(int Id, string Subject, string Type, string Status, string StatusColor, string TypeColor, int? AssignedUserId, string? AssigneeName, DateTime UpdatedAt);

public record WorkspaceDashboardView(
    int TotalTickets,
    int OpenTickets,
    int ResolvedTickets,
    int ActiveMembers,
    IReadOnlyList<TicketStatus> StatusList,
    IReadOnlyList<TicketType> TypeList,
    IReadOnlyList<TicketPriority> PriorityList,
    IReadOnlyDictionary<string, int> PriorityCounts,
    string PrimaryColor,
    bool PrimaryIsHex,
    string SuccessColor,
    bool SuccessIsHex,
    IReadOnlyList<User> WorkspaceMembers,
    IReadOnlyList<Team> WorkspaceTeams,
    IReadOnlyList<DashboardActivityPoint> ActivitySeries,
    IReadOnlyList<DashboardMemberStat> TopMembers,
    string AvgResolutionLabel,
    IReadOnlyList<DashboardTicketListItem> RecentTickets,
    bool CanViewDashboard,
    bool CanViewTickets,
    string TicketViewScope);

public interface IWorkspaceDashboardViewService
{
    Task<WorkspaceDashboardView> BuildAsync(
        int workspaceId,
        int userId,
        string scope,
        IReadOnlyList<int> teamIds,
        int rangeDays,
        string assignmentFilter);
}
