namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TeamRepository(TickfloDbContext dbContext) : ITeamRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<Team?> FindByIdAsync(int id)
        => this.dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id);

    public Task<Team?> FindByNameAsync(int workspaceId, string name)
        => this.dbContext.Teams.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name == name);

    public Task<List<Team>> ListForWorkspaceAsync(int workspaceId)
        => this.dbContext.Teams.Where(t => t.WorkspaceId == workspaceId).OrderBy(t => t.Name).ToListAsync();

    public async Task<Team> AddAsync(int workspaceId, string name, string? description, int createdBy)
    {
        var team = new Team
        {
            WorkspaceId = workspaceId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        this.dbContext.Teams.Add(team);
        await this.dbContext.SaveChangesAsync();
        return team;
    }

    public async Task UpdateAsync(Team team)
    {
        this.dbContext.Teams.Update(team);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var team = await this.dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team != null)
        {
            // Remove members first to maintain FK integrity
            var members = this.dbContext.TeamMembers.Where(m => m.TeamId == id);
            this.dbContext.TeamMembers.RemoveRange(members);
            this.dbContext.Teams.Remove(team);
            await this.dbContext.SaveChangesAsync();
        }
    }
}
