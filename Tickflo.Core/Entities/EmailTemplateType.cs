namespace Tickflo.Core.Entities;

/// <summary>
/// Email template type IDs.
/// These correspond to the template_type_id column in the email_templates table.
/// </summary>
public enum EmailTemplateType
{
    /// <summary>
    /// Email Confirmation Thank You page content (shown after user confirms email)
    /// </summary>
    EmailConfirmationThankYou = 1,

    /// <summary>
    /// Workspace Invite - New User (sent when inviting a new user to workspace)
    /// </summary>
    WorkspaceInviteNewUser = 2,

    /// <summary>
    /// Email Confirmation Request (sent when user needs to confirm their email)
    /// </summary>
    EmailConfirmationRequest = 3,

    /// <summary>
    /// Workspace Invite Resend (sent when resending workspace invitation)
    /// </summary>
    WorkspaceInviteResend = 4,

    /// <summary>
    /// Signup Welcome (sent after user signs up)
    /// </summary>
    SignupWelcome = 5,

    /// <summary>
    /// Forgot Password / Password Reset (sent when user requests password reset)
    /// </summary>
    ForgotPassword = 6,

    /// <summary>
    /// Confirm New Email (sent to new email address during email change)
    /// </summary>
    ConfirmNewEmail = 7,

    /// <summary>
    /// Revert Email Change (sent to old email address with option to cancel change)
    /// </summary>
    RevertEmailChange = 8,

    /// <summary>
    /// Workspace Member Removal (sent when user is removed from workspace)
    /// </summary>
    WorkspaceMemberRemoval = 9,

    /// <summary>
    /// Workspace Invite - Existing User (sent when inviting an existing user to workspace)
    /// </summary>
    WorkspaceInviteExistingUser = 10
}
