namespace Tickflo.Core.Services;

public class WorkspaceReportRunExecuteData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportRunExecuteViewService
{
    Task<WorkspaceReportRunExecuteData> BuildAsync(int workspaceId, int userId);
}
