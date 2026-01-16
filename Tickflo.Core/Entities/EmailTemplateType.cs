namespace Tickflo.Core.Entities;

/// <summary>
/// Constants for email template type IDs.
/// These correspond to the template_type_id column in the email_templates table.
/// </summary>
public static class EmailTemplateType
{
    /// <summary>
    /// Email Confirmation Thank You page content (shown after user confirms email)
    /// </summary>
    public const int EmailConfirmationThankYou = 1;

    /// <summary>
    /// Workspace Invite - New User (sent when inviting a new user to workspace)
    /// </summary>
    public const int WorkspaceInviteNewUser = 2;

    /// <summary>
    /// Email Confirmation Request (sent when user needs to confirm their email)
    /// </summary>
    public const int EmailConfirmationRequest = 3;

    /// <summary>
    /// Workspace Invite Resend (sent when resending workspace invitation)
    /// </summary>
    public const int WorkspaceInviteResend = 4;

    /// <summary>
    /// Signup Welcome (sent after user signs up)
    /// </summary>
    public const int SignupWelcome = 5;

    /// <summary>
    /// Forgot Password / Password Reset (sent when user requests password reset)
    /// </summary>
    public const int ForgotPassword = 6;

    /// <summary>
    /// Confirm New Email (sent to new email address during email change)
    /// </summary>
    public const int ConfirmNewEmail = 7;

    /// <summary>
    /// Revert Email Change (sent to old email address with option to cancel change)
    /// </summary>
    public const int RevertEmailChange = 8;

    /// <summary>
    /// Workspace Member Removal (sent when user is removed from workspace)
    /// </summary>
    public const int WorkspaceMemberRemoval = 9;
}
