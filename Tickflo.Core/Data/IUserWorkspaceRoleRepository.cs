namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IUserWorkspaceRoleRepository
{
    public Task<bool> IsAdminAsync(int userId, int workspaceId);
    public Task<List<string>> GetRoleNamesAsync(int userId, int workspaceId);
    public Task<UserWorkspaceRole> AddAsync(UserWorkspaceRole userWorkspaceRole);
    public Task<List<Role>> GetRolesAsync(int userId, int workspaceId);
    public Task RemoveAsync(int userId, int workspaceId, int roleId);
    public Task<int> CountAssignmentsForRoleAsync(int workspaceId, int roleId);
}
