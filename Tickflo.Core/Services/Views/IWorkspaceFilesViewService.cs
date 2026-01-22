namespace Tickflo.Core.Services.Views;

public class WorkspaceFilesViewData
{
    public bool CanViewFiles { get; set; }
}

public interface IWorkspaceFilesViewService
{
    public Task<WorkspaceFilesViewData> BuildAsync(int workspaceId, int userId);
}


