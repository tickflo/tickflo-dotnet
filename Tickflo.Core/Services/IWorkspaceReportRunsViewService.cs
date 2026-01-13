using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceReportRunsViewData
{
    public bool CanViewReports { get; set; }
    public Report? Report { get; set; }
    public List<ReportRun> Runs { get; set; } = new();
}

public interface IWorkspaceReportRunsViewService
{
    Task<WorkspaceReportRunsViewData> BuildAsync(int workspaceId, int userId, int reportId);
}
