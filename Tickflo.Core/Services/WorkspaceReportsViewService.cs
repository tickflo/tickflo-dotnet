using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceReportsViewService : IWorkspaceReportsViewService
{
    private readonly IRolePermissionRepository _rolePermissions;
    private readonly IReportQueryService _reportQueryService;
    private readonly IWorkspaceAccessService _workspaceAccessService;

    public WorkspaceReportsViewService(
        IRolePermissionRepository rolePermissions,
        IReportQueryService reportQueryService,
        IWorkspaceAccessService workspaceAccessService)
    {
        _rolePermissions = rolePermissions;
        _reportQueryService = reportQueryService;
        _workspaceAccessService = workspaceAccessService;
    }

    public async Task<WorkspaceReportsViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportsViewData();

        // Get user's effective permissions for reports
        var permissions = await _rolePermissions.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (permissions.TryGetValue("reports", out var reportPermissions))
        {
            data.CanCreateReports = reportPermissions.CanCreate;
            data.CanEditReports = reportPermissions.CanEdit;
        }

        // Load reports list
        var reports = await _reportQueryService.ListReportsAsync(workspaceId);
        data.Reports = reports
            .Select(r => new ReportSummary
            {
                Id = r.Id,
                Name = r.Name,
                Ready = r.Ready,
                LastRun = r.LastRun
            })
            .ToList();

        return data;
    }
}
