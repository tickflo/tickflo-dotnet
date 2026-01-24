namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IRoleRepository
{
    public Task<Role?> FindByNameAsync(int workspaceId, string name);
    public Task<Role> AddAsync(Role role);
    public Task<List<Role>> ListForWorkspaceAsync(int workspaceId);
    public Task<Role?> FindByIdAsync(int id);
    public Task UpdateAsync(Role role);
    public Task DeleteAsync(int id);
}
