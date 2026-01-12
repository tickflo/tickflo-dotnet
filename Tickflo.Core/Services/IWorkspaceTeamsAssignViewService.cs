using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceTeamsAssignViewData
{
    public bool CanViewTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public Team? Team { get; set; }
    public List<User> WorkspaceUsers { get; set; } = new();
    public List<User> Members { get; set; } = new();
}

public interface IWorkspaceTeamsAssignViewService
{
    Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId);
}
