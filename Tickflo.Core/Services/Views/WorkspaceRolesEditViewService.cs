using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Views;

public class WorkspaceRolesEditViewService : IWorkspaceRolesEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IRolePermissionRepository _rolePerms;

    public WorkspaceRolesEditViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRoleRepository roleRepo,
        IRolePermissionRepository rolePerms)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _roleRepo = roleRepo;
        _rolePerms = rolePerms;
    }

    public async Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0)
    {
        var data = new WorkspaceRolesEditViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;

        if (!isAdmin) return data;

        if (roleId > 0)
        {
            var role = await _roleRepo.FindByIdAsync(roleId);
            if (role != null && role.WorkspaceId == workspaceId)
            {
                data.ExistingRole = role;
                var perms = await _rolePerms.ListByRoleAsync(roleId);
                data.ExistingPermissions = perms.ToList();
            }
        }

        return data;
    }
}


