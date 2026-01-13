using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ITeamMemberRepository
{
    Task<List<User>> ListMembersAsync(int teamId);
    Task<List<Team>> ListTeamsForUserAsync(int workspaceId, int userId);
    Task AddAsync(int teamId, int userId);
    Task RemoveAsync(int teamId, int userId);
}
