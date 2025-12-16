using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IRoleRepository
{
    Task<Role?> FindByNameAsync(int workspaceId, string name);
    Task<Role> AddAsync(int workspaceId, string name, bool admin, int createdBy);
    Task<List<Role>> ListForWorkspaceAsync(int workspaceId);
    Task<Role?> FindByIdAsync(int id);
    Task UpdateAsync(Role role);
    Task DeleteAsync(int id);
}
