namespace Tickflo.Core.Services;

public class WorkspaceReportDeleteViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportDeleteViewService
{
    Task<WorkspaceReportDeleteViewData> BuildAsync(int workspaceId, int userId);
}
