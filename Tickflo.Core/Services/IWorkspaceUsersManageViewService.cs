namespace Tickflo.Core.Services;

public class WorkspaceUsersManageViewData
{
    public bool CanEditUsers { get; set; }
}

public interface IWorkspaceUsersManageViewService
{
    Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId);
}
