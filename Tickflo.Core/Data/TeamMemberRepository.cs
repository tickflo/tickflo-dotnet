namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public interface ITeamMemberRepository
{
    public Task<List<User>> ListMembersAsync(int teamId);
    public Task<List<Team>> ListTeamsForUserAsync(int workspaceId, int userId);
    public Task AddAsync(int teamId, int userId);
    public Task RemoveAsync(int teamId, int userId);
}


public class TeamMemberRepository(TickfloDbContext dbContext) : ITeamMemberRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<List<User>> ListMembersAsync(int teamId)
    {
        var userIds = await this.dbContext.TeamMembers.Where(tm => tm.TeamId == teamId).Select(tm => tm.UserId).ToListAsync();
        return await this.dbContext.Users.Where(u => userIds.Contains(u.Id)).OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<List<Team>> ListTeamsForUserAsync(int workspaceId, int userId)
    {
        var teamIds = await this.dbContext.TeamMembers.Where(tm => tm.UserId == userId).Select(tm => tm.TeamId).ToListAsync();
        return await this.dbContext.Teams.Where(t => t.WorkspaceId == workspaceId && teamIds.Contains(t.Id)).OrderBy(t => t.Name).ToListAsync();
    }

    public async Task AddAsync(int teamId, int userId)
    {
        var existing = await this.dbContext.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        if (existing != null)
        {
            return;
        }

        this.dbContext.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = userId, JoinedAt = DateTime.UtcNow });
        await this.dbContext.SaveChangesAsync();
    }

    public async Task RemoveAsync(int teamId, int userId)
    {
        var existing = await this.dbContext.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        if (existing == null)
        {
            return;
        }

        this.dbContext.TeamMembers.Remove(existing);
        await this.dbContext.SaveChangesAsync();
    }
}
