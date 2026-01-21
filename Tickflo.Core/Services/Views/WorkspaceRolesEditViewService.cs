namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceRolesEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRoleRepository roleRepo,
    IRolePermissionRepository rolePerms) : IWorkspaceRolesEditViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRoleRepository _roleRepo = roleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;

    public async Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0)
    {
        var data = new WorkspaceRolesEditViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;

        if (!isAdmin)
        {
            return data;
        }

        if (roleId > 0)
        {
            var role = await this._roleRepo.FindByIdAsync(roleId);
            if (role != null && role.WorkspaceId == workspaceId)
            {
                data.ExistingRole = role;
                var perms = await this._rolePerms.ListByRoleAsync(roleId);
                data.ExistingPermissions = [.. perms];
            }
        }

        return data;
    }
}


