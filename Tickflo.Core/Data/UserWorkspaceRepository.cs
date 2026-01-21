namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserWorkspaceRepository(TickfloDbContext db) : IUserWorkspaceRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task AddAsync(UserWorkspace userWorkspace)
    {
        this._db.UserWorkspaces.Add(userWorkspace);
        await this._db.SaveChangesAsync();
    }

    public Task<UserWorkspace?> FindAcceptedForUserAsync(int userId) => this._db.UserWorkspaces.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.Accepted);

    public Task<List<UserWorkspace>> FindForUserAsync(int userId) => this._db.UserWorkspaces
            .Where(uw => uw.UserId == userId)
            .ToListAsync();

    public Task<List<UserWorkspace>> FindForWorkspaceAsync(int workspaceId) => this._db.UserWorkspaces
            .Where(uw => uw.WorkspaceId == workspaceId)
            .OrderByDescending(uw => uw.CreatedAt)
            .ToListAsync();

    public Task<UserWorkspace?> FindAsync(int userId, int workspaceId) => this._db.UserWorkspaces.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId);

    public async Task UpdateAsync(UserWorkspace uw)
    {
        this._db.UserWorkspaces.Update(uw);
        await this._db.SaveChangesAsync();
    }
}
