namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

public class WorkspaceReportRunsViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IReportRunService reportRunService) : IWorkspaceReportRunsViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IReportRunService _reportRunService = reportRunService;

    public async Task<WorkspaceReportRunsViewData> BuildAsync(int workspaceId, int userId, int reportId)
    {
        var data = new WorkspaceReportRunsViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);

        if (!data.CanViewReports)
        {
            return data;
        }

        var (Report, Runs) = await this._reportRunService.GetReportRunsAsync(workspaceId, reportId, 100);
        if (Report != null)
        {
            data.Report = Report;
            data.Runs = [.. Runs];
        }

        return data;
    }
}



