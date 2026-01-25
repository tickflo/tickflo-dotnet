namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Entities;

/// <summary>
/// Service for managing user invitations and onboarding workflows.
/// </summary>
public interface IUserInvitationService
{
    /// <summary>
    /// Invites a user to a workspace with an auto-generated temporary password.
    /// </summary>
    /// <param name="workspaceId">Target workspace</param>
    /// <param name="email">User's email address</param>
    /// <param name="invitedByUserId">User sending the invitation</param>
    /// <param name="roleIds">Role IDs to assign upon acceptance</param>
    public Task InviteUserAsync(
        int workspaceId,
        string email,
        int invitedByUserId,
        List<int> roleIds);

    /// <summary>
    /// Resends an invitation email with a new confirmation code.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="userId">User to resend invitation to</param>
    /// <param name="resentByUserId">User triggering the resend</param>
    public Task ResendInvitationAsync(int workspaceId, int userId, int resentByUserId);

    /// <summary>
    /// Accepts a workspace invitation.
    /// </summary>
    /// <param name="slug">Workspace to accept</param>
    /// <param name="userId">User accepting the invitation</param>
    public Task AcceptInvitationAsync(string slug, int userId);
    public Task DeclineInvitationAsync(string slug, int userId);
}

/// <summary>
/// Result of a user invitation operation.
/// </summary>
public class UserInvitationResult
{
    public User User { get; set; } = null!;
    public string AcceptLink { get; set; } = string.Empty;
    public bool IsNewUser { get; set; }
}


