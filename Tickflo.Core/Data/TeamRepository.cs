namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class TeamRepository(TickfloDbContext db) : ITeamRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<Team?> FindByIdAsync(int id)
        => this._db.Teams.FirstOrDefaultAsync(t => t.Id == id);

    public Task<Team?> FindByNameAsync(int workspaceId, string name)
        => this._db.Teams.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name == name);

    public Task<List<Team>> ListForWorkspaceAsync(int workspaceId)
        => this._db.Teams.Where(t => t.WorkspaceId == workspaceId).OrderBy(t => t.Name).ToListAsync();

    public async Task<Team> AddAsync(int workspaceId, string name, string? description, int createdBy)
    {
        var team = new Team
        {
            WorkspaceId = workspaceId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description!.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        this._db.Teams.Add(team);
        await this._db.SaveChangesAsync();
        return team;
    }

    public async Task UpdateAsync(Team team)
    {
        this._db.Teams.Update(team);
        await this._db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var team = await this._db.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team != null)
        {
            // Remove members first to maintain FK integrity
            var members = this._db.TeamMembers.Where(m => m.TeamId == id);
            this._db.TeamMembers.RemoveRange(members);
            this._db.Teams.Remove(team);
            await this._db.SaveChangesAsync();
        }
    }
}
