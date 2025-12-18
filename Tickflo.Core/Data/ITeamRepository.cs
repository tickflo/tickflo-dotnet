using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITeamRepository
{
    Task<Team?> FindByIdAsync(int id);
    Task<Team?> FindByNameAsync(int workspaceId, string name);
    Task<List<Team>> ListForWorkspaceAsync(int workspaceId);
    Task<Team> AddAsync(int workspaceId, string name, string? description, int createdBy);
    Task UpdateAsync(Team team);
    Task DeleteAsync(int id);
}
