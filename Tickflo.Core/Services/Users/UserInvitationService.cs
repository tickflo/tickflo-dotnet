namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;

/// <summary>
/// Service for managing user invitations and onboarding workflows.
/// </summary>
public class UserInvitationService(
    IUserRepository userRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IEmailSendService emailSendService,
    IWorkspaceRepository workspaceRepository) : IUserInvitationService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;

    public async Task<UserInvitationResult> InviteUserAsync(
        int workspaceId,
        string email,
        int invitedByUserId,
        List<int> roleIds)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required");
        }

        if (roleIds == null || roleIds.Count == 0)
        {
            throw new InvalidOperationException("At least one role is required");
        }

        email = email.Trim().ToLowerInvariant();

        // Get workspace for email template
        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId) ?? throw new InvalidOperationException("Workspace not found");

        // Check if user already exists
        var existingUser = await this.userRepository.FindByEmailAsync(email);
        User user;
        bool isNewUser;

        if (existingUser == null)
        {
            isNewUser = true;

            // Generate email confirmation code
            var emailConfirmationCode = TokenGenerator.GenerateToken(16);

            // Create new user without password - they will set it after confirming email
            user = new User
            {
                Name = email.Split('@')[0], // Default name from email
                Email = email,
                PasswordHash = null, // No password yet - user will set it after email confirmation
                EmailConfirmed = false,
                EmailConfirmationCode = emailConfirmationCode,
                SystemAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            await this.userRepository.AddAsync(user);
        }
        else
        {
            isNewUser = false;
            user = existingUser;
        }

        // Generate confirmation code for invitation acceptance
        var confirmationCode = GenerateConfirmationCode();

        // Create or update workspace membership
        var existingMembership = await this.userWorkspaceRepository.FindAsync(user.Id, workspaceId);

        if (existingMembership == null)
        {
            var membership = new UserWorkspace
            {
                UserId = user.Id,
                WorkspaceId = workspaceId,
                Accepted = false,
                CreatedBy = invitedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            await this.userWorkspaceRepository.AddAsync(membership);
        }

        // Assign roles
        foreach (var roleId in roleIds)
        {
            // Verify role exists and belongs to workspace
            var role = await this.roleRepository.FindByIdAsync(roleId);
            if (role != null && role.WorkspaceId == workspaceId)
            {
                // Add role using repository method
                await this.userWorkspaceRoleRepository.AddAsync(user.Id, workspaceId, roleId, invitedByUserId);
            }
        }

        // Build invitation result
        var result = new UserInvitationResult
        {
            User = user,
            ConfirmationCode = confirmationCode,
            TemporaryPassword = string.Empty, // No longer using temporary passwords
            ConfirmationLink = $"/email-confirmation/confirm?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(user.EmailConfirmationCode ?? string.Empty)}",
            AcceptLink = $"/accept-invite?code={confirmationCode}",
            ResetPasswordLink = $"/reset-password?email={Uri.EscapeDataString(email)}",
            SetPasswordLink = $"/set-password?userId={user.Id}",
            IsNewUser = isNewUser
        };

        // Send appropriate email based on user type
        await this.SendInvitationEmailAsync(workspace, user, result, isNewUser);

        return result;
    }

    public async Task<string> ResendInvitationAsync(int workspaceId, int userId, int resentByUserId)
    {
        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("User is not invited to this workspace");

        // Generate new confirmation code
        var confirmationCode = GenerateConfirmationCode();

        // Note: ConfirmationCode is not stored in UserWorkspace entity currently
        // This would need to be added to the entity and database schema
        // For now, return the new code for the caller to handle

        return confirmationCode;
    }

    public async Task AcceptInvitationAsync(int workspaceId, int userId)
    {
        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("Invitation not found");

        membership.Accepted = true;
        await this.userWorkspaceRepository.UpdateAsync(membership);
    }

    private async Task SendInvitationEmailAsync(
        Workspace workspace,
        User user,
        UserInvitationResult invitationResult,
        bool isNewUser)
    {
        var variables = new Dictionary<string, string>
        {
            { "WORKSPACE_NAME", workspace.Name },
            { "USER_NAME", user.Name },
            { "ACCEPT_LINK", invitationResult.AcceptLink }
        };

        EmailTemplateType templateType;

        if (isNewUser)
        {
            // For new users, include confirmation link and set password link
            templateType = EmailTemplateType.WorkspaceInviteNewUser;
            variables["CONFIRMATION_LINK"] = invitationResult.ConfirmationLink;
            variables["SET_PASSWORD_LINK"] = invitationResult.SetPasswordLink;
        }
        else
        {
            // For existing users, just send the accept link
            templateType = EmailTemplateType.WorkspaceInviteExistingUser;
        }

        await this.emailSendService.SendAsync(
            user.Email,
            templateType,
            variables,
            workspace.Id);
    }

    private static string GenerateConfirmationCode() => Guid.NewGuid().ToString("N");
}


