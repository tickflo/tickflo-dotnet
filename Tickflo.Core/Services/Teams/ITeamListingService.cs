namespace Tickflo.Core.Services.Teams;

using Tickflo.Core.Entities;

public interface ITeamListingService
{
    /// <summary>
    /// Gets teams for a workspace with member counts.
    /// </summary>
    public Task<(IReadOnlyList<Team> Teams, IReadOnlyDictionary<int, int> MemberCounts)> GetListAsync(int workspaceId);
}


