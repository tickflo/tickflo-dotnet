using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Users;

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
    /// <param name="roleIds">Optional role IDs to assign upon acceptance</param>
    /// <returns>Invitation details including user, confirmation code, and temp password</returns>
    Task<UserInvitationResult> InviteUserAsync(
        int workspaceId, 
        string email, 
        int invitedByUserId, 
        List<int>? roleIds = null);

    /// <summary>
    /// Resends an invitation email with a new confirmation code.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="userId">User to resend invitation to</param>
    /// <param name="resentByUserId">User triggering the resend</param>
    /// <returns>New confirmation code</returns>
    Task<string> ResendInvitationAsync(int workspaceId, int userId, int resentByUserId);

    /// <summary>
    /// Accepts a workspace invitation.
    /// </summary>
    /// <param name="workspaceId">Workspace to accept</param>
    /// <param name="userId">User accepting the invitation</param>
    Task AcceptInvitationAsync(int workspaceId, int userId);

    /// <summary>
    /// Generates a secure temporary password.
    /// </summary>
    /// <param name="length">Password length</param>
    /// <returns>Temporary password string</returns>
    string GenerateTemporaryPassword(int length = 12);
}

/// <summary>
/// Result of a user invitation operation.
/// </summary>
public class UserInvitationResult
{
    public User User { get; set; } = null!;
    public string ConfirmationCode { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string ConfirmationLink { get; set; } = string.Empty;
    public string AcceptLink { get; set; } = string.Empty;
    public string ResetPasswordLink { get; set; } = string.Empty;
}


