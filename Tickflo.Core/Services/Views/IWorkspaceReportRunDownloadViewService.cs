namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceReportRunDownloadViewData
{
    public bool CanViewReports { get; set; }
    public ReportRun? Run { get; set; }
}

public interface IWorkspaceReportRunDownloadViewService
{
    public Task<WorkspaceReportRunDownloadViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId);
}


