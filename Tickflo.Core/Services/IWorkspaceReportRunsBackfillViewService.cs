namespace Tickflo.Core.Services;

public class WorkspaceReportRunsBackfillViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportRunsBackfillViewService
{
    Task<WorkspaceReportRunsBackfillViewData> BuildAsync(int workspaceId, int userId);
}
