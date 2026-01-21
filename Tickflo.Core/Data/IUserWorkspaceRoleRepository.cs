namespace Tickflo.Core.Data;

public interface IUserWorkspaceRoleRepository
{
    public Task<bool> IsAdminAsync(int userId, int workspaceId);
    public Task<List<string>> GetRoleNamesAsync(int userId, int workspaceId);
    public Task AddAsync(int userId, int workspaceId, int roleId, int createdBy);
    public Task<List<Entities.Role>> GetRolesAsync(int userId, int workspaceId);
    public Task RemoveAsync(int userId, int workspaceId, int roleId);
    public Task<int> CountAssignmentsForRoleAsync(int workspaceId, int roleId);
}
