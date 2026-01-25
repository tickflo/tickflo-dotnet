namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class WorkspaceRolesAssignViewData
{
    public bool IsAdmin { get; set; }
    public List<User> Members { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
    public Dictionary<int, List<Role>> UserRoles { get; set; } = [];
}

public interface IWorkspaceRolesAssignViewService
{
    public Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId);
}


public class WorkspaceRolesAssignViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepo) : IWorkspaceRolesAssignViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly IRoleRepository roleRepository = roleRepo;

    public async Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesAssignViewData();

        var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        data.IsAdmin = isAdmin;
        if (!isAdmin)
        {
            return data;
        }

        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var id in userIds)
        {
            var user = await this.userRepository.FindByIdAsync(id);
            if (user != null)
            {
                data.Members.Add(user);
            }
        }

        data.Roles = await this.roleRepository.ListForWorkspaceAsync(workspaceId);

        foreach (var id in userIds)
        {
            var roles = await this.userWorkspaceRoleRepository.GetRolesAsync(id, workspaceId);
            data.UserRoles[id] = roles;
        }

        return data;
    }
}


