namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceTeamsEditViewData
{
    public bool CanViewTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public bool CanCreateTeams { get; set; }
    public Team? ExistingTeam { get; set; }
    public List<User> WorkspaceUsers { get; set; } = [];
    public List<int> ExistingMemberIds { get; set; } = [];
}

public interface IWorkspaceTeamsEditViewService
{
    public Task<WorkspaceTeamsEditViewData> BuildAsync(int workspaceId, int userId, int teamId = 0);
}


