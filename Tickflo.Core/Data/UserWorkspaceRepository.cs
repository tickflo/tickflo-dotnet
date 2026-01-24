namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserWorkspaceRepository(TickfloDbContext dbContext) : IUserWorkspaceRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task AddAsync(UserWorkspace userWorkspace)
    {
        this.dbContext.UserWorkspaces.Add(userWorkspace);
        await this.dbContext.SaveChangesAsync();
    }

    public Task<UserWorkspace?> FindAcceptedForUserAsync(int userId) => this.dbContext.UserWorkspaces.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.Accepted);

    public Task<List<UserWorkspace>> FindForUserAsync(int userId) => this.dbContext.UserWorkspaces
            .Where(uw => uw.UserId == userId)
            .Include(uw => uw.Workspace)
            .ToListAsync();

    public Task<List<UserWorkspace>> FindForWorkspaceAsync(int workspaceId) => this.dbContext.UserWorkspaces
            .Where(uw => uw.WorkspaceId == workspaceId)
            .OrderByDescending(uw => uw.CreatedAt)
            .ToListAsync();

    public Task<UserWorkspace?> FindAsync(int userId, int workspaceId) => this.dbContext.UserWorkspaces.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId);

    public async Task UpdateAsync(UserWorkspace uw)
    {
        this.dbContext.UserWorkspaces.Update(uw);
        await this.dbContext.SaveChangesAsync();
    }
}
