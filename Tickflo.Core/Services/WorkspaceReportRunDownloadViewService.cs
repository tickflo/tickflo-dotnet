using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceReportRunDownloadViewService : IWorkspaceReportRunDownloadViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IReportRunService _reportRunService;

    public WorkspaceReportRunDownloadViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IReportRunService reportRunService)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _reportRunService = reportRunService;
    }

    public async Task<WorkspaceReportRunDownloadViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId)
    {
        var data = new WorkspaceReportRunDownloadViewData();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports) return data;

        var run = await _reportRunService.GetRunAsync(workspaceId, runId);
        if (run == null || run.ReportId != reportId) return data;
        data.Run = run;
        return data;
    }
}
