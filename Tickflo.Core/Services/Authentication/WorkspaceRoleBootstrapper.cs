namespace Tickflo.Core.Services.Authentication;

using Tickflo.Core.Data;

public class WorkspaceRoleBootstrapper(IRoleRepository roles, IUserWorkspaceRoleRepository userWorkspaceRoles) : IWorkspaceRoleBootstrapper
{
    private readonly IRoleRepository _roles = roles;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoles = userWorkspaceRoles;

    public async Task BootstrapAdminAsync(int workspaceId, int creatorUserId)
    {
        var existing = await this._roles.FindByNameAsync(workspaceId, "Admin");
        var adminRole = existing ?? await this._roles.AddAsync(workspaceId, "Admin", true, creatorUserId);
        await this._userWorkspaceRoles.AddAsync(creatorUserId, workspaceId, adminRole.Id, creatorUserId);
    }
}


