using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceRolesAssignViewService : IWorkspaceRolesAssignViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;

    public WorkspaceRolesAssignViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IUserWorkspaceRepository userWorkspaces,
        IUserRepository userRepo,
        IRoleRepository roleRepo)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _userWorkspaces = userWorkspaces;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
    }

    public async Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesAssignViewData();

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;
        if (!isAdmin) return data;

        var memberships = await _userWorkspaces.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var id in userIds)
        {
            var u = await _userRepo.FindByIdAsync(id);
            if (u != null) data.Members.Add(u);
        }

        data.Roles = await _roleRepo.ListForWorkspaceAsync(workspaceId);

        foreach (var id in userIds)
        {
            var roles = await _userWorkspaceRoleRepo.GetRolesAsync(id, workspaceId);
            data.UserRoles[id] = roles;
        }

        return data;
    }
}
