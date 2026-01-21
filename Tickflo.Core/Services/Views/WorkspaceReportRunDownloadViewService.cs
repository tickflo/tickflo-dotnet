namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

public class WorkspaceReportRunDownloadViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IReportRunService reportRunService) : IWorkspaceReportRunDownloadViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IReportRunService _reportRunService = reportRunService;

    public async Task<WorkspaceReportRunDownloadViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId)
    {
        var data = new WorkspaceReportRunDownloadViewData();
        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports)
        {
            return data;
        }

        var run = await this._reportRunService.GetRunAsync(workspaceId, runId);
        if (run == null || run.ReportId != reportId)
        {
            return data;
        }

        data.Run = run;
        return data;
    }
}



