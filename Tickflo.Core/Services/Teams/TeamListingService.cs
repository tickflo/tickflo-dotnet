using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Teams;

public class TeamListingService : ITeamListingService
{
    private readonly ITeamRepository _teamRepo;
    private readonly ITeamMemberRepository _memberRepo;

    public TeamListingService(
        ITeamRepository teamRepo,
        ITeamMemberRepository memberRepo)
    {
        _teamRepo = teamRepo;
        _memberRepo = memberRepo;
    }

    public async Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId)
    {
        var teams = await _teamRepo.ListForWorkspaceAsync(workspaceId);
        var memberCounts = new Dictionary<int, int>();
        
        foreach (var team in teams)
        {
            var members = await _memberRepo.ListMembersAsync(team.Id);
            memberCounts[team.Id] = members.Count;
        }
        
        return (teams.AsReadOnly(), memberCounts.AsReadOnly());
    }
}


