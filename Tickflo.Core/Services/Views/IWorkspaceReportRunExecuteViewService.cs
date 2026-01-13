namespace Tickflo.Core.Services.Views;

public class WorkspaceReportRunExecuteData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportRunExecuteViewService
{
    Task<WorkspaceReportRunExecuteData> BuildAsync(int workspaceId, int userId);
}


