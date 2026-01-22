namespace Tickflo.Core.Services.Authentication;

using Tickflo.Core.Data;

public class WorkspaceRoleBootstrapper(IRoleRepository roleRepository, IUserWorkspaceRoleRepository userWorkspaceRoleRepository) : IWorkspaceRoleBootstrapper
{
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;

    public async Task BootstrapAdminAsync(int workspaceId, int creatorUserId)
    {
        var existing = await this.roleRepository.FindByNameAsync(workspaceId, "Admin");
        var adminRole = existing ?? await this.roleRepository.AddAsync(workspaceId, "Admin", true, creatorUserId);
        await this.userWorkspaceRoleRepository.AddAsync(creatorUserId, workspaceId, adminRole.Id, creatorUserId);
    }
}


