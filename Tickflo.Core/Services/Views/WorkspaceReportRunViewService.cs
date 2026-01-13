using Tickflo.Core.Data;

using Tickflo.Core.Services.Reporting;

namespace Tickflo.Core.Services.Views;

public class WorkspaceReportRunViewService : IWorkspaceReportRunViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IReportingService _reportingService;

    public WorkspaceReportRunViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IReportRepository reportRepo,
        IReportRunRepository reportRunRepo,
        IReportingService reportingService)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
        _reportingService = reportingService;
    }

    public async Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take)
    {
        var data = new WorkspaceReportRunViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports) return data;

        var rep = await _reportRepo.FindAsync(workspaceId, reportId);
        if (rep == null) return data;
        data.Report = rep;

        var run = await _reportRunRepo.FindAsync(workspaceId, runId);
        if (run == null || run.ReportId != reportId) return data;
        data.Run = run;

        var pageResult = await _reportingService.GetRunPageAsync(run, page, take);
        data.PageData = new ReportRunPageData
        {
            Page = pageResult.Page,
            Take = pageResult.Take,
            TotalRows = pageResult.TotalRows,
            TotalPages = pageResult.TotalPages,
            FromRow = pageResult.FromRow,
            ToRow = pageResult.ToRow,
            HasContent = pageResult.HasContent,
            Headers = pageResult.Headers.ToList(),
            Rows = pageResult.Rows.Select(r => r.ToList()).ToList()
        };

        return data;
    }
}



