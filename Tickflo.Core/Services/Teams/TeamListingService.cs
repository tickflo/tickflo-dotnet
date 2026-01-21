namespace Tickflo.Core.Services.Teams;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class TeamListingService(
    ITeamRepository teamRepository,
    ITeamMemberRepository memberRepo) : ITeamListingService
{
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly ITeamMemberRepository _memberRepo = memberRepo;

    public async Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId)
    {
        var teams = await this.teamRepository.ListForWorkspaceAsync(workspaceId);
        var memberCounts = new Dictionary<int, int>();

        foreach (var team in teams)
        {
            var members = await this._memberRepo.ListMembersAsync(team.Id);
            memberCounts[team.Id] = members.Count;
        }

        return (teams.AsReadOnly(), memberCounts.AsReadOnly());
    }
}


