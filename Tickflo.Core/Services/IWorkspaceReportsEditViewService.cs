using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceReportsEditViewData
{
    public bool CanViewReports { get; set; }
    public bool CanEditReports { get; set; }
    public bool CanCreateReports { get; set; }
    public Report? ExistingReport { get; set; }
    public IReadOnlyDictionary<string, string[]> Sources { get; set; } = new Dictionary<string, string[]>();
}

public interface IWorkspaceReportsEditViewService
{
    Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0);
}
