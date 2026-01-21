namespace Tickflo.Core.Services.Views;

public class WorkspaceUsersManageViewData
{
    public bool CanEditUsers { get; set; }
}

public interface IWorkspaceUsersManageViewService
{
    public Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId);
}


