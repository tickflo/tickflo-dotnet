namespace Tickflo.Core.Services;

public class WorkspaceUsersInviteViewData
{
    public bool CanViewUsers { get; set; }
    public bool CanCreateUsers { get; set; }
}

public interface IWorkspaceUsersInviteViewService
{
    Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId);
}
