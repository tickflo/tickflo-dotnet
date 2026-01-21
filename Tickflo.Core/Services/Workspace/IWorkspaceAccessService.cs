namespace Tickflo.Core.Services.Workspace;

using Tickflo.Core.Data;

/// <summary>
/// Service for verifying user access and permissions within workspaces.
/// Centralizes authorization logic for workspace operations.
/// </summary>
public interface IWorkspaceAccessService
{
    /// <summary>
    /// Verifies that a user has access to a specific workspace.
    /// </summary>
    /// <param name="userId">The user to check</param>
    /// <param name="workspaceId">The workspace to verify access to</param>
    /// <returns>True if user has accepted membership in workspace</returns>
    public Task<bool> UserHasAccessAsync(int userId, int workspaceId);

    /// <summary>
    /// Verifies that a user is an admin of a specific workspace.
    /// </summary>
    /// <param name="userId">The user to check</param>
    /// <param name="workspaceId">The workspace to check admin status in</param>
    /// <returns>True if user is workspace admin</returns>
    public Task<bool> UserIsWorkspaceAdminAsync(int userId, int workspaceId);

    /// <summary>
    /// Gets effective permissions for a user in a workspace.
    /// Combines role-based permissions with admin override.
    /// </summary>
    /// <param name="workspaceId">The workspace to check permissions in</param>
    /// <param name="userId">The user to get permissions for</param>
    /// <returns>Dictionary mapping permission names to permission objects</returns>
    public Task<Dictionary<string, EffectiveSectionPermission>> GetUserPermissionsAsync(int workspaceId, int userId);

    /// <summary>
    /// Checks if a user can perform a specific action in a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="userId">The user to check</param>
    /// <param name="resourceType">The resource type (e.g., "tickets", "contacts")</param>
    /// <param name="action">The action type (e.g., "view", "create", "edit")</param>
    /// <returns>True if user can perform the action</returns>
    public Task<bool> CanUserPerformActionAsync(int workspaceId, int userId, string resourceType, string action);

    /// <summary>
    /// Gets the ticket view scope for a user (what tickets they can see).
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="userId">The user to check</param>
    /// <param name="isAdmin">Whether user is workspace admin</param>
    /// <returns>View scope label (e.g., "all", "assigned", "created")</returns>
    public Task<string> GetTicketViewScopeAsync(int workspaceId, int userId, bool isAdmin);

    /// <summary>
    /// Ensures user has admin access or throws UnauthorizedAccessException.
    /// </summary>
    /// <param name="userId">The user to verify</param>
    /// <param name="workspaceId">The workspace to verify admin access to</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not admin</exception>
    public Task EnsureAdminAccessAsync(int userId, int workspaceId);

    /// <summary>
    /// Ensures user has access to workspace or throws UnauthorizedAccessException.
    /// </summary>
    /// <param name="userId">The user to verify</param>
    /// <param name="workspaceId">The workspace to verify access to</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user has no access</exception>
    public Task EnsureWorkspaceAccessAsync(int userId, int workspaceId);
}



