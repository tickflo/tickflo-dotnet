using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceUsersInviteViewService : IWorkspaceUsersInviteViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;

    public WorkspaceUsersInviteViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
    }

    public async Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersInviteViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewUsers = isAdmin || (eff.TryGetValue("users", out var up) && up.CanView);
        data.CanCreateUsers = isAdmin || (eff.TryGetValue("users", out var up2) && up2.CanCreate);

        return data;
    }
}
