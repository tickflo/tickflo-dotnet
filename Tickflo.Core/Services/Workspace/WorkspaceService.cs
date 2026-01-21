namespace Tickflo.Core.Services.Workspace;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using WorkspaceEntity = Entities.Workspace;

/// <summary>
/// Implementation of IWorkspaceService.
/// Provides workspace lookup and membership operations.
/// </summary>
public class WorkspaceService(
    IWorkspaceRepository workspaceRepository,
    IUserWorkspaceRepository userWorkspaceRepository) : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;

    public async Task<List<UserWorkspace>> GetUserWorkspacesAsync(int userId) => await this.userWorkspaceRepository.FindForUserAsync(userId);

    public async Task<List<UserWorkspace>> GetAcceptedWorkspacesAsync(int userId)
    {
        var all = await this.GetUserWorkspacesAsync(userId);
        return [.. all.Where(uw => uw.Accepted)];
    }

    public async Task<WorkspaceEntity?> GetWorkspaceBySlugAsync(string slug) => await this._workspaceRepository.FindBySlugAsync(slug);

    public async Task<WorkspaceEntity?> GetWorkspaceAsync(int workspaceId) => await this._workspaceRepository.FindByIdAsync(workspaceId);

    public async Task<bool> UserHasMembershipAsync(int userId, int workspaceId)
    {
        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspaceId);
        return membership?.Accepted ?? false;
    }

    public async Task<UserWorkspace?> GetMembershipAsync(int userId, int workspaceId) => await this.userWorkspaceRepository.FindAsync(userId, workspaceId);
}





