using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IUserWorkspaceRoleRepository
{
    Task<bool> IsAdminAsync(int userId, int workspaceId);
    Task<List<string>> GetRoleNamesAsync(int userId, int workspaceId);
    Task AddAsync(int userId, int workspaceId, int roleId, int createdBy);
    Task<List<Entities.Role>> GetRolesAsync(int userId, int workspaceId);
    Task RemoveAsync(int userId, int workspaceId, int roleId);
    Task<int> CountAssignmentsForRoleAsync(int workspaceId, int roleId);
}
