using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IUserWorkspaceRepository
{
    Task AddAsync(UserWorkspace userWorkspace);
    Task<UserWorkspace?> FindAcceptedForUserAsync(int userId);
    Task<List<UserWorkspace>> FindForUserAsync(int userId);
}
