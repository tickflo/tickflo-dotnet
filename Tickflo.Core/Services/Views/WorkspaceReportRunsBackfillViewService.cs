namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceReportRunsBackfillViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceReportRunsBackfillViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<WorkspaceReportRunsBackfillViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportRunsBackfillViewData();
        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}


