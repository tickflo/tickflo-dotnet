namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;

public class WorkspaceReportsEditViewData
{
    public bool CanViewReports { get; set; }
    public bool CanEditReports { get; set; }
    public bool CanCreateReports { get; set; }
    public Report? ExistingReport { get; set; }
    public IReadOnlyDictionary<string, string[]> Sources { get; set; } = new Dictionary<string, string[]>();
}

public interface IWorkspaceReportsEditViewService
{
    public Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0);
}


public class WorkspaceReportsEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IReportRepository reporyRepository,
    IReportingService reportingService) : IWorkspaceReportsEditViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IReportRepository reporyRepository = reporyRepository;
    private readonly IReportingService reportingService = reportingService;

    public async Task<WorkspaceReportsEditViewData> BuildAsync(int workspaceId, int userId, int reportId = 0)
    {
        var data = new WorkspaceReportsEditViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);

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

        data.Sources = this.reportingService.GetAvailableSources();

        if (reportId > 0)
        {
            data.ExistingReport = await this.reporyRepository.FindAsync(workspaceId, reportId);
        }

        return data;
    }
}



