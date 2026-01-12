using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class ReportRunPageData
{
    public int Page { get; set; }
    public int Take { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public class WorkspaceReportRunViewData
{
    public bool CanViewReports { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }
    public ReportRunPageData? PageData { get; set; }
}

public interface IWorkspaceReportRunViewService
{
    Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take);
}
