namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITeamRepository
{
    public Task<Team?> FindByIdAsync(int id);
    public Task<Team?> FindByNameAsync(int workspaceId, string name);
    public Task<List<Team>> ListForWorkspaceAsync(int workspaceId);
    public Task<Team> AddAsync(int workspaceId, string name, string? description, int createdBy);
    public Task UpdateAsync(Team team);
    public Task DeleteAsync(int id);
}
