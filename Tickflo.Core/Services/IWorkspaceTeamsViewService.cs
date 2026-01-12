using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public interface IWorkspaceTeamsViewService
{
    Task<WorkspaceTeamsViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceTeamsViewData
{
    public List<Team> Teams { get; set; } = new();
    public Dictionary<int, int> MemberCounts { get; set; } = new();
    public bool CanCreateTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public bool CanViewTeams { get; set; }
}
