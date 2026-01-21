namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceReportRunsViewData
{
    public bool CanViewReports { get; set; }
    public Report? Report { get; set; }
    public List<ReportRun> Runs { get; set; } = [];
}

public interface IWorkspaceReportRunsViewService
{
    public Task<WorkspaceReportRunsViewData> BuildAsync(int workspaceId, int userId, int reportId);
}


