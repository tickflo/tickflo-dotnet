namespace Tickflo.Core.Services.Teams;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface ITeamListingService
{
    /// <summary>
    /// Gets teams for a workspace with member counts.
    /// </summary>
    public Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId);
}


public class TeamListingService(
    ITeamRepository teamRepository,
    ITeamMemberRepository teamMemberRepository) : ITeamListingService
{
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMemberRepository;

    public async Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId)
    {
        var teams = await this.teamRepository.ListForWorkspaceAsync(workspaceId);
        var memberCounts = new Dictionary<int, int>();

        foreach (var team in teams)
        {
            var members = await this.teamMemberRepository.ListMembersAsync(team.Id);
            memberCounts[team.Id] = members.Count;
        }

        return (teams.AsReadOnly(), memberCounts.AsReadOnly());
    }
}


