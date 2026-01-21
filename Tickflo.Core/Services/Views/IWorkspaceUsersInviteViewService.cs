namespace Tickflo.Core.Services.Views;

public class WorkspaceUsersInviteViewData
{
    public bool CanViewUsers { get; set; }
    public bool CanCreateUsers { get; set; }
}

public interface IWorkspaceUsersInviteViewService
{
    public Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId);
}


