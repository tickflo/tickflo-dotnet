namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;

public class ReportRunPageData
{
    public int Page { get; set; }
    public int Take { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }
    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
}

public class WorkspaceReportRunViewData
{
    public bool CanViewReports { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }
    public ReportRunPageData? PageData { get; set; }
}

public interface IWorkspaceReportRunViewService
{
    public Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take);
}


public class WorkspaceReportRunViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IReportRepository reporyRepository,
    IReportRunRepository reportRunRepository,
    IReportingService reportingService) : IWorkspaceReportRunViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IReportRepository reporyRepository = reporyRepository;
    private readonly IReportRunRepository reportRunRepository = reportRunRepository;
    private readonly IReportingService reportingService = reportingService;

    public async Task<WorkspaceReportRunViewData> BuildAsync(int workspaceId, int userId, int reportId, int runId, int page, int take)
    {
        var data = new WorkspaceReportRunViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanView);
        if (!data.CanViewReports)
        {
            return data;
        }

        var rep = await this.reporyRepository.FindAsync(workspaceId, reportId);
        if (rep == null)
        {
            return data;
        }

        data.Report = rep;

        var run = await this.reportRunRepository.FindAsync(workspaceId, runId);
        if (run == null || run.ReportId != reportId)
        {
            return data;
        }

        data.Run = run;

        var pageResult = await this.reportingService.GetRunPageAsync(run, page, take);
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



