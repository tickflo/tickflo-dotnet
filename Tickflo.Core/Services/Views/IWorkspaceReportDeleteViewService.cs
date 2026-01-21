namespace Tickflo.Core.Services.Views;

public class WorkspaceReportDeleteViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportDeleteViewService
{
    public Task<WorkspaceReportDeleteViewData> BuildAsync(int workspaceId, int userId);
}


