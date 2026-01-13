using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

namespace Tickflo.Core.Services.Views;

public class WorkspaceReportsEditViewService : IWorkspaceReportsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IReportRepository _reportRepo;
    private readonly IReportingService _reportingService;

    public WorkspaceReportsEditViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IReportRepository reportRepo,
        IReportingService reportingService)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _reportRepo = reportRepo;
        _reportingService = reportingService;
    }

    public async Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0)
    {
        var data = new WorkspaceReportsEditViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

        if (isAdmin)
        {
            data.CanViewReports = data.CanEditReports = data.CanCreateReports = true;
        }
        else if (eff.TryGetValue("reports", out var rp))
        {
            data.CanViewReports = rp.CanView;
            data.CanEditReports = rp.CanEdit;
            data.CanCreateReports = rp.CanCreate;
        }

        data.Sources = _reportingService.GetAvailableSources();

        if (reportId > 0)
        {
            data.ExistingReport = await _reportRepo.FindAsync(workspaceId, reportId);
        }

        return data;
    }
}



