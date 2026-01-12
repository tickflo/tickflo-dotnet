using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceReportRunExecuteViewService : IWorkspaceReportRunExecuteViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;

    public WorkspaceReportRunExecuteViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
    }

    public async Task<WorkspaceReportRunExecuteData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceReportRunExecuteData();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanEditReports = isAdmin || (eff.TryGetValue("reports", out var rp) && rp.CanEdit);
        return data;
    }
}
