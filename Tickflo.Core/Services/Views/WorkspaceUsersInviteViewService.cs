namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceUsersInviteViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms) : IWorkspaceUsersInviteViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;

    public async Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersInviteViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewUsers = isAdmin || (eff.TryGetValue("users", out var up) && up.CanView);
        data.CanCreateUsers = isAdmin || (eff.TryGetValue("users", out var up2) && up2.CanCreate);

        return data;
    }
}


