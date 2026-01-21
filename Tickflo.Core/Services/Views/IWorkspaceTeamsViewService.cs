namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public interface IWorkspaceTeamsViewService
{
    public Task<WorkspaceTeamsViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceTeamsViewData
{
    public List<Team> Teams { get; set; } = [];
    public Dictionary<int, int> MemberCounts { get; set; } = [];
    public bool CanCreateTeams { get; set; }
    public bool CanEditTeams { get; set; }
    public bool CanViewTeams { get; set; }
}


