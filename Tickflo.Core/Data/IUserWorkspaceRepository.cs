namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IUserWorkspaceRepository
{
    public Task AddAsync(UserWorkspace userWorkspace);
    public Task<UserWorkspace?> FindAcceptedForUserAsync(int userId);
    public Task<List<UserWorkspace>> FindForUserAsync(int userId);
    public Task<List<UserWorkspace>> FindForWorkspaceAsync(int workspaceId);
    public Task<UserWorkspace?> FindAsync(int userId, int workspaceId);
    public Task UpdateAsync(UserWorkspace uw);
}
