namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Workspace;

public class WorkspaceReportsViewService(
    IRolePermissionRepository rolePermissions,
    IReportQueryService reportQueryService,
    IWorkspaceAccessService workspaceAccessService) : IWorkspaceReportsViewService
{
    private readonly IRolePermissionRepository _rolePermissions = rolePermissions;
    private readonly IReportQueryService _reportQueryService = reportQueryService;
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;

    public async Task<WorkspaceReportsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportsViewData();

        // Get user's effective permissions for reports
        var permissions = await this._rolePermissions.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (permissions.TryGetValue("reports", out var reportPermissions))
        {
            data.CanCreateReports = reportPermissions.CanCreate;
            data.CanEditReports = reportPermissions.CanEdit;
        }

        // Load reports list
        var reports = await this._reportQueryService.ListReportsAsync(workspaceId);
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



