using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public class WorkspaceReportRunDownloadViewData
{
    public bool CanViewReports { get; set; }
    public ReportRun? Run { get; set; }
}

public interface IWorkspaceReportRunDownloadViewService
{
    Task<WorkspaceReportRunDownloadViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId);
}


