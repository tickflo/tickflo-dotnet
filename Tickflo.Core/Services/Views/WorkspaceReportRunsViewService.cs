using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

namespace Tickflo.Core.Services.Views;

public class WorkspaceReportRunsViewService : IWorkspaceReportRunsViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IReportRunService _reportRunService;

    public WorkspaceReportRunsViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IReportRunService reportRunService)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _reportRunService = reportRunService;
    }

    public async Task<WorkspaceReportRunsViewData> BuildAsync(int workspaceId, int userId, int reportId)
    {
        var data = new WorkspaceReportRunsViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);

        if (!data.CanViewReports) return data;

        var result = await _reportRunService.GetReportRunsAsync(workspaceId, reportId, 100);
        if (result.Report != null)
        {
            data.Report = result.Report;
            data.Runs = result.Runs.ToList();
        }

        return data;
    }
}



