namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class WorkspaceRolesEditViewData
{
    public bool IsAdmin { get; set; }
    public Role? ExistingRole { get; set; }
    public List<EffectiveSectionPermission> ExistingPermissions { get; set; } = [];
}

public interface IWorkspaceRolesEditViewService
{
    public Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0);
}


public class WorkspaceRolesEditViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRoleRepository roleRepo,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceRolesEditViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRoleRepository roleRepository = roleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0)
    {
        var data = new WorkspaceRolesEditViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;

        if (!isAdmin)
        {
            return data;
        }

        if (roleId > 0)
        {
            var role = await this.roleRepository.FindByIdAsync(roleId);
            if (role != null && role.WorkspaceId == workspaceId)
            {
                data.ExistingRole = role;
                var perms = await this.rolePermissionRepository.ListByRoleAsync(roleId);
                data.ExistingPermissions = [.. perms];
            }
        }

        return data;
    }
}


