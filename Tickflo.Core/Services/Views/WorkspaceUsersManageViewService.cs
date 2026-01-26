namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
public class WorkspaceUsersManageViewData
{
    public bool CanEditUsers { get; set; }
}

public interface IWorkspaceUsersManageViewService
{
    public Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceUsersManageViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceUsersManageViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<WorkspaceUsersManageViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersManageViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        if (isAdmin)
        {
            data.CanEditUsers = true;
        }
        else
        {
            var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            data.CanEditUsers = eff.TryGetValue("users", out var up) && up.CanEdit;
        }

        return data;
    }
}


