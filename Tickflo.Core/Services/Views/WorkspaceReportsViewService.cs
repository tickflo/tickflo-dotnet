namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;
public interface IWorkspaceReportsViewService
{
    public Task<WorkspaceReportsViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceReportsViewData
{
    public List<ReportSummary> Reports { get; set; } = [];
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


public class WorkspaceReportsViewService(
    IRolePermissionRepository rolePermissionRepository,
    IReportQueryService reportQueryService) : IWorkspaceReportsViewService
{
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IReportQueryService reportQueryService = reportQueryService;

    public async Task<WorkspaceReportsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportsViewData();

        // Get user's effective permissions for reports
        var permissions = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (permissions.TryGetValue("reports", out var reportPermissions))
        {
            data.CanCreateReports = reportPermissions.CanCreate;
            data.CanEditReports = reportPermissions.CanEdit;
        }

        // Load reports list
        var reports = await this.reportQueryService.ListReportsAsync(workspaceId);
        data.Reports = [.. reports
            .Select(r => new ReportSummary
            {
                Id = r.Id,
                Name = r.Name,
                Ready = r.Ready,
                LastRun = r.LastRun
            })];

        return data;
    }
}



