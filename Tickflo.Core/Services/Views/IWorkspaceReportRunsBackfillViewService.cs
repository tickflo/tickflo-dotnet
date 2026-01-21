namespace Tickflo.Core.Services.Views;

public class WorkspaceReportRunsBackfillViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportRunsBackfillViewService
{
    public Task<WorkspaceReportRunsBackfillViewData> BuildAsync(int workspaceId, int userId);
}


