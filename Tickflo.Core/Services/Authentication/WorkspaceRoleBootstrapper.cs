using System.Threading.Tasks;
using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Authentication;

public class WorkspaceRoleBootstrapper(IRoleRepository roles, IUserWorkspaceRoleRepository userWorkspaceRoles) : IWorkspaceRoleBootstrapper
{
    private readonly IRoleRepository _roles = roles;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoles = userWorkspaceRoles;

    public async Task BootstrapAdminAsync(int workspaceId, int creatorUserId)
    {
        var existing = await _roles.FindByNameAsync(workspaceId, "Admin");
        var adminRole = existing ?? await _roles.AddAsync(workspaceId, "Admin", true, creatorUserId);
        await _userWorkspaceRoles.AddAsync(creatorUserId, workspaceId, adminRole.Id, creatorUserId);
    }
}


