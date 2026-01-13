using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IUserWorkspaceRepository
{
    Task AddAsync(UserWorkspace userWorkspace);
    Task<UserWorkspace?> FindAcceptedForUserAsync(int userId);
    Task<List<UserWorkspace>> FindForUserAsync(int userId);
    Task<List<UserWorkspace>> FindForWorkspaceAsync(int workspaceId);
    Task<UserWorkspace?> FindAsync(int userId, int workspaceId);
    Task UpdateAsync(UserWorkspace uw);
}
