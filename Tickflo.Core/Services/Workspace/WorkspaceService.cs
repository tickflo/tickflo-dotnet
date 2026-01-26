namespace Tickflo.Core.Services.Workspace;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;
/// <summary>
/// Implementation of IWorkspaceService.
/// Provides workspace lookup and membership operations.
/// </summary>
using WorkspaceEntity = Entities.Workspace;

/// <summary>
/// Service for managing workspace-related operations.
/// Handles workspace lookup and membership management.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Gets all workspaces a user is a member of.
    /// </summary>
    /// <param name="userId">The user to get workspaces for</param>
    /// <returns>List of workspace memberships</returns>
    public Task<List<UserWorkspace>> GetUserWorkspacesAsync(int userId);

    /// <summary>
    /// Gets all accepted workspace memberships for a user.
    /// </summary>
    /// <param name="userId">The user to get memberships for</param>
    /// <returns>List of accepted workspace memberships</returns>
    public Task<List<UserWorkspace>> GetAcceptedWorkspacesAsync(int userId);

    /// <summary>
    /// Gets a workspace by its slug.
    /// </summary>
    /// <param name="slug">The workspace slug</param>
    /// <returns>The workspace, or null if not found</returns>
    public Task<WorkspaceEntity?> GetWorkspaceBySlugAsync(string slug);

    /// <summary>
    /// Gets a workspace by ID.
    /// </summary>
    /// <param name="workspaceId">The workspace ID</param>
    /// <returns>The workspace, or null if not found</returns>
    public Task<WorkspaceEntity?> GetWorkspaceAsync(int workspaceId);

    /// <summary>
    /// Verifies that a user has accepted membership in a workspace.
    /// </summary>
    /// <param name="userId">The user to check</param>
    /// <param name="workspaceId">The workspace to verify membership in</param>
    /// <returns>True if user has accepted membership</returns>
    public Task<bool> UserHasMembershipAsync(int userId, int workspaceId);

    /// <summary>
    /// Gets the user's membership in a workspace.
    /// </summary>
    /// <param name="userId">The user</param>
    /// <param name="workspaceId">The workspace</param>
    /// <returns>The membership, or null if not found</returns>
    public Task<UserWorkspace?> GetMembershipAsync(int userId, int workspaceId);
}

public class WorkspaceService(
    IWorkspaceRepository workspaceRepository,
    IUserWorkspaceRepository userWorkspaceRepository) : IWorkspaceService
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;

    public async Task<List<UserWorkspace>> GetUserWorkspacesAsync(int userId) => await this.userWorkspaceRepository.FindForUserAsync(userId);

    public async Task<List<UserWorkspace>> GetAcceptedWorkspacesAsync(int userId)
    {
        var all = await this.GetUserWorkspacesAsync(userId);
        return [.. all.Where(uw => uw.Accepted)];
    }

    public async Task<WorkspaceEntity?> GetWorkspaceBySlugAsync(string slug) => await this.workspaceRepository.FindBySlugAsync(slug);

    public async Task<WorkspaceEntity?> GetWorkspaceAsync(int workspaceId) => await this.workspaceRepository.FindByIdAsync(workspaceId);

    public async Task<bool> UserHasMembershipAsync(int userId, int workspaceId)
    {
        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspaceId);
        return membership?.Accepted ?? false;
    }

    public async Task<UserWorkspace?> GetMembershipAsync(int userId, int workspaceId) => await this.userWorkspaceRepository.FindAsync(userId, workspaceId);
}





