namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Workspace;
public class WorkspaceFilesViewData
{
    public bool CanViewFiles { get; set; }
}

public interface IWorkspaceFilesViewService
{
    public Task<WorkspaceFilesViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceFilesViewService(IWorkspaceAccessService workspaceAccessService) : IWorkspaceFilesViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceFilesViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceFilesViewData
        {
            CanViewFiles = await this.workspaceAccessService.UserHasAccessAsync(userId, workspaceId)
        };
        return data;
    }
}



