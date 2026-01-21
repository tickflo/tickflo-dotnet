namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceUsersManageViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms) : IWorkspaceUsersManageViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;

    public async Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersManageViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanEditUsers = true;
        }
        else
        {
            var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            data.CanEditUsers = eff.TryGetValue("users", out var up) && up.CanEdit;
        }

        return data;
    }
}


