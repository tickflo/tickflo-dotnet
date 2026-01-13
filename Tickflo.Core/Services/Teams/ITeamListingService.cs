using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Teams;

public interface ITeamListingService
{
    /// <summary>
    /// Gets teams for a workspace with member counts.
    /// </summary>
    Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId);
}


