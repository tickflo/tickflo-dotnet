using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for managing role assignments and role operations.
/// Centralizes role management business logic.
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Assigns a role to a user in a workspace.
    /// </summary>
    /// <param name="userId">The user to assign the role to</param>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to assign</param>
    /// <param name="assignedByUserId">The user performing the assignment (for auditing)</param>
    /// <returns>The created role assignment</returns>
    /// <exception cref="InvalidOperationException">If role doesn't belong to workspace or user already has role</exception>
    Task<UserWorkspaceRole> AssignRoleToUserAsync(int userId, int workspaceId, int roleId, int assignedByUserId);

    /// <summary>
    /// Removes a role assignment from a user.
    /// </summary>
    /// <param name="userId">The user to remove the role from</param>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to remove</param>
    /// <returns>True if assignment was removed, false if not found</returns>
    Task<bool> RemoveRoleFromUserAsync(int userId, int workspaceId, int roleId);

    /// <summary>
    /// Counts how many users have a specific role in a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to count assignments for</param>
    /// <returns>Number of users with this role</returns>
    Task<int> CountRoleAssignmentsAsync(int workspaceId, int roleId);

    /// <summary>
    /// Verifies that a role belongs to a specific workspace.
    /// </summary>
    /// <param name="roleId">The role to verify</param>
    /// <param name="workspaceId">The workspace that should own the role</param>
    /// <returns>True if role belongs to workspace</returns>
    Task<bool> RoleBelongsToWorkspaceAsync(int roleId, int workspaceId);

    /// <summary>
    /// Gets all roles for a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace to list roles for</param>
    /// <returns>List of roles in the workspace</returns>
    Task<List<Role>> GetWorkspaceRolesAsync(int workspaceId);

    /// <summary>
    /// Gets all roles assigned to a user in a workspace.
    /// </summary>
    /// <param name="userId">The user to get roles for</param>
    /// <param name="workspaceId">The workspace context</param>
    /// <returns>List of roles assigned to the user</returns>
    Task<List<Role>> GetUserRolesAsync(int userId, int workspaceId);

    /// <summary>
    /// Ensures a role can be deleted (has no assignments).
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="roleId">The role to check</param>
    /// <param name="roleName">The role name (for error messages)</param>
    /// <exception cref="InvalidOperationException">If role has assignments</exception>
    Task EnsureRoleCanBeDeletedAsync(int workspaceId, int roleId, string roleName);
}
