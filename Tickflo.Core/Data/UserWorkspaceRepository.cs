using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class UserWorkspaceRepository(TickfloDbContext db) : IUserWorkspaceRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task AddAsync(UserWorkspace userWorkspace)
    {
        _db.UserWorkspaces.Add(userWorkspace);
        await _db.SaveChangesAsync();
    }

    public Task<UserWorkspace?> FindAcceptedForUserAsync(int userId)
    {
        return _db.UserWorkspaces.FirstOrDefaultAsync(uw => uw.UserId == userId && uw.Accepted);
    }

    public Task<List<UserWorkspace>> FindForUserAsync(int userId)
    {
        return _db.UserWorkspaces
            .Where(uw => uw.UserId == userId)
            .ToListAsync();
    }
}
