namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

public class WorkspaceReportRunViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    IReportRepository reportRepo,
    IReportRunRepository reportRunRepo,
    IReportingService reportingService) : IWorkspaceReportRunViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly IReportRepository _reportRepo = reportRepo;
    private readonly IReportRunRepository _reportRunRepo = reportRunRepo;
    private readonly IReportingService _reportingService = reportingService;

    public async Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take)
    {
        var data = new WorkspaceReportRunViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports)
        {
            return data;
        }

        var rep = await this._reportRepo.FindAsync(workspaceId, reportId);
        if (rep == null)
        {
            return data;
        }

        data.Report = rep;

        var run = await this._reportRunRepo.FindAsync(workspaceId, runId);
        if (run == null || run.ReportId != reportId)
        {
            return data;
        }

        data.Run = run;

        var pageResult = await this._reportingService.GetRunPageAsync(run, page, take);
        data.PageData = new ReportRunPageData
        {
            Page = pageResult.Page,
            Take = pageResult.Take,
            TotalRows = pageResult.TotalRows,
            TotalPages = pageResult.TotalPages,
            FromRow = pageResult.FromRow,
            ToRow = pageResult.ToRow,
            HasContent = pageResult.HasContent,
            Headers = [.. pageResult.Headers],
            Rows = [.. pageResult.Rows.Select(r => r.ToList())]
        };

        return data;
    }
}



