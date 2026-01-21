namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Entities;

/// <summary>
/// Handles user onboarding and workspace management workflows.
/// </summary>
public interface IUserOnboardingService
{
    /// <summary>
    /// Invites a user to join a workspace with a specific role.
    /// </summary>
    /// <param name="workspaceId">Target workspace</param>
    /// <param name="email">User email to invite</param>
    /// <param name="roleId">Role to assign</param>
    /// <param name="invitedByUserId">User sending invitation</param>
    /// <returns>The user workspace assignment</returns>
    public Task<UserWorkspace> InviteUserToWorkspaceAsync(int workspaceId, string email, int roleId, int invitedByUserId);

    /// <summary>
    /// User accepts a pending workspace invitation.
    /// </summary>
    /// <param name="userId">User accepting invitation</param>
    /// <param name="workspaceId">Workspace to join</param>
    /// <returns>The accepted user workspace assignment</returns>
    public Task<UserWorkspace> AcceptInvitationAsync(int userId, int workspaceId);

    /// <summary>
    /// User declines a pending workspace invitation.
    /// </summary>
    /// <param name="userId">User declining invitation</param>
    /// <param name="workspaceId">Workspace to decline</param>
    public Task DeclineInvitationAsync(int userId, int workspaceId);

    /// <summary>
    /// Removes a user from a workspace.
    /// </summary>
    /// <param name="userId">User to remove</param>
    /// <param name="workspaceId">Workspace to remove from</param>
    /// <param name="removedByUserId">User performing the removal</param>
    public Task RemoveUserFromWorkspaceAsync(int userId, int workspaceId, int removedByUserId);
}
