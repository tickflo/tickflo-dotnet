namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ITeamMemberRepository
{
    public Task<List<User>> ListMembersAsync(int teamId);
    public Task<List<Team>> ListTeamsForUserAsync(int workspaceId, int userId);
    public Task AddAsync(int teamId, int userId);
    public Task RemoveAsync(int teamId, int userId);
}
