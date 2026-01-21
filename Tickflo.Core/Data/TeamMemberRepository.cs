namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TeamMemberRepository(TickfloDbContext db) : ITeamMemberRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task<List<User>> ListMembersAsync(int teamId)
    {
        var userIds = await this._db.TeamMembers.Where(tm => tm.TeamId == teamId).Select(tm => tm.UserId).ToListAsync();
        return await this._db.Users.Where(u => userIds.Contains(u.Id)).OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<List<Team>> ListTeamsForUserAsync(int workspaceId, int userId)
    {
        var teamIds = await this._db.TeamMembers.Where(tm => tm.UserId == userId).Select(tm => tm.TeamId).ToListAsync();
        return await this._db.Teams.Where(t => t.WorkspaceId == workspaceId && teamIds.Contains(t.Id)).OrderBy(t => t.Name).ToListAsync();
    }

    public async Task AddAsync(int teamId, int userId)
    {
        var existing = await this._db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        if (existing != null)
        {
            return;
        }

        this._db.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = userId, JoinedAt = DateTime.UtcNow });
        await this._db.SaveChangesAsync();
    }

    public async Task RemoveAsync(int teamId, int userId)
    {
        var existing = await this._db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        if (existing == null)
        {
            return;
        }

        this._db.TeamMembers.Remove(existing);
        await this._db.SaveChangesAsync();
    }
}
