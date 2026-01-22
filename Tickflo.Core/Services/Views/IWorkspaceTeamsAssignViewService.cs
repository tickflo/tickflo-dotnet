namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceTeamsAssignViewData
{
    public bool CanViewTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public Team? Team { get; set; }
    public List<User> WorkspaceUsers { get; set; } = [];
    public List<User> Members { get; set; } = [];
}

public interface IWorkspaceTeamsAssignViewService
{
    public Task<WorkspaceTeamsAssignViewData> BuildAsync(int workspaceId, int userId, int teamId);
}


