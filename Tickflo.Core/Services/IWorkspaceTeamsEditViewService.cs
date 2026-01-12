using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceTeamsEditViewData
{
    public bool CanViewTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public bool CanCreateTeams { get; set; }
    public Team? ExistingTeam { get; set; }
    public List<User> WorkspaceUsers { get; set; } = new();
    public List<int> ExistingMemberIds { get; set; } = new();
}

public interface IWorkspaceTeamsEditViewService
{
    Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0);
}
