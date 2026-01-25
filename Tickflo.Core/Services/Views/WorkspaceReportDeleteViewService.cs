namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
public class WorkspaceReportDeleteViewData
{
    public bool CanEditReports { get; set; }
}

public interface IWorkspaceReportDeleteViewService
{
    public Task<WorkspaceReportDeleteViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceReportDeleteViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceReportDeleteViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<WorkspaceReportDeleteViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportDeleteViewData();
        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}


