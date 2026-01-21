namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceReportRunExecuteViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms) : IWorkspaceReportRunExecuteViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;

    public async Task<WorkspaceReportRunExecuteData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportRunExecuteData();
        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}


