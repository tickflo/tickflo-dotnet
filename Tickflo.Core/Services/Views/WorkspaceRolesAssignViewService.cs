namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

public class WorkspaceRolesAssignViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IUserWorkspaceRepository userWorkspaces,
    IUserRepository userRepo,
    IRoleRepository roleRepo) : IWorkspaceRolesAssignViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces = userWorkspaces;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IRoleRepository _roleRepo = roleRepo;

    public async Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesAssignViewData();

        var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;
        if (!isAdmin)
        {
            return data;
        }

        var memberships = await this._userWorkspaces.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var id in userIds)
        {
            var u = await this._userRepo.FindByIdAsync(id);
            if (u != null)
            {
                data.Members.Add(u);
            }
        }

        data.Roles = await this._roleRepo.ListForWorkspaceAsync(workspaceId);

        foreach (var id in userIds)
        {
            var roles = await this._userWorkspaceRoleRepo.GetRolesAsync(id, workspaceId);
            data.UserRoles[id] = roles;
        }

        return data;
    }
}


