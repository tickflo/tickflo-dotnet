namespace Tickflo.Core.Entities;

/// <summary>
/// Email template type IDs.
/// These correspond to the template_type_id column in the email_templates table.
/// </summary>
public enum EmailTemplateType
{
    Signup = 1,
    ForgotPassword = 2,
    ConfirmNewEmail = 3,
    RevertEmailChange = 4,
    WorkspaceInviteNewUser = 5,
    WorkspaceInviteExistingUser = 6,
    WorkspaceRemoval = 7,
}
