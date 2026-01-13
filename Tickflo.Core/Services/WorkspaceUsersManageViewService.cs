using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceUsersManageViewService : IWorkspaceUsersManageViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;

    public WorkspaceUsersManageViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
    }

    public async Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersManageViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanEditUsers = true;
        }
        else
        {
            var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            data.CanEditUsers = eff.TryGetValue("users", out var up) && up.CanEdit;
        }

        return data;
    }
}
