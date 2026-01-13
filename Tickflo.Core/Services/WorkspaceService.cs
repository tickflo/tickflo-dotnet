using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Implementation of IWorkspaceService.
/// Provides workspace lookup and membership operations.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUserWorkspaceRepository _userWorkspaceRepository;

    public WorkspaceService(
        IWorkspaceRepository workspaceRepository,
        IUserWorkspaceRepository userWorkspaceRepository)
    {
        _workspaceRepository = workspaceRepository;
        _userWorkspaceRepository = userWorkspaceRepository;
    }

    public async Task<List<UserWorkspace>> GetUserWorkspacesAsync(int userId)
    {
        return await _userWorkspaceRepository.FindForUserAsync(userId);
    }

    public async Task<List<UserWorkspace>> GetAcceptedWorkspacesAsync(int userId)
    {
        var all = await GetUserWorkspacesAsync(userId);
        return all.Where(uw => uw.Accepted).ToList();
    }

    public async Task<Workspace?> GetWorkspaceBySlugAsync(string slug)
    {
        return await _workspaceRepository.FindBySlugAsync(slug);
    }

    public async Task<Workspace?> GetWorkspaceAsync(int workspaceId)
    {
        return await _workspaceRepository.FindByIdAsync(workspaceId);
    }

    public async Task<bool> UserHasMembershipAsync(int userId, int workspaceId)
    {
        var membership = await _userWorkspaceRepository.FindAsync(userId, workspaceId);
        return membership?.Accepted ?? false;
    }

    public async Task<UserWorkspace?> GetMembershipAsync(int userId, int workspaceId)
    {
        return await _userWorkspaceRepository.FindAsync(userId, workspaceId);
    }
}
