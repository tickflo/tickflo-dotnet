namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

public class WorkspaceReportsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    IReportRepository reportRepo,
    IReportingService reportingService) : IWorkspaceReportsEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly IReportRepository _reportRepo = reportRepo;
    private readonly IReportingService _reportingService = reportingService;

    public async Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0)
    {
        var data = new WorkspaceReportsEditViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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

        data.Sources = this._reportingService.GetAvailableSources();

        if (reportId > 0)
        {
            data.ExistingReport = await this._reportRepo.FindAsync(workspaceId, reportId);
        }

        return data;
    }
}



