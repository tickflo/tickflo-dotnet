namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
public class WorkspaceUsersInviteViewData
{
    public bool CanViewUsers { get; set; }
    public bool CanCreateUsers { get; set; }
}

public interface IWorkspaceUsersInviteViewService
{
    public Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceUsersInviteViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceUsersInviteViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<WorkspaceUsersInviteViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceUsersInviteViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        data.CanViewUsers = isAdmin || (eff.TryGetValue("users", out var up) && up.CanView);
        data.CanCreateUsers = isAdmin || (eff.TryGetValue("users", out var up2) && up2.CanCreate);

        return data;
    }
}


