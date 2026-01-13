namespace Tickflo.Core.Services.Views;

public class WorkspaceFilesViewData
{
    public bool CanViewFiles { get; set; }
}

public interface IWorkspaceFilesViewService
{
    Task<WorkspaceFilesViewData> BuildAsync(int workspaceId, int userId);
}


