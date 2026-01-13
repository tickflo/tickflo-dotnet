using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceFilesViewService : IWorkspaceFilesViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService;

    public WorkspaceFilesViewService(IWorkspaceAccessService workspaceAccessService)
    {
        _workspaceAccessService = workspaceAccessService;
    }

    public async Task<WorkspaceFilesViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceFilesViewData();
        data.CanViewFiles = await _workspaceAccessService.UserHasAccessAsync(userId, workspaceId);
        return data;
    }
}
