using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public interface IWorkspaceReportsViewService
{
    Task<WorkspaceReportsViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceReportsViewData
{
    public List<ReportSummary> Reports { get; set; } = new();
    public bool CanCreateReports { get; set; }
    public bool CanEditReports { get; set; }
}

public class ReportSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Ready { get; set; }
    public DateTime? LastRun { get; set; }
}
