namespace Tickflo.Core.Services.Users;

using System.Security.Cryptography;
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
        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
        if (workspace == null)
        {
            throw new InvalidOperationException("Workspace not found");
        }

        // Check if user already exists
        var existingUser = await this.userRepository.FindByEmailAsync(email);
        User user;
        bool isNewUser;
        string? temporaryPassword = null;

        if (existingUser == null)
        {
            isNewUser = true;
            
            // Generate temporary password for new users
            temporaryPassword = this.GenerateTemporaryPassword(12);

            // Generate email confirmation code
            var emailConfirmationCode = TokenGenerator.GenerateToken(16);

            // Create new user
            user = new User
            {
                Name = email.Split('@')[0], // Default name from email
                Email = email,
                PasswordHash = this.passwordHasher.Hash(temporaryPassword),
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
            TemporaryPassword = temporaryPassword ?? string.Empty,
            ConfirmationLink = $"/confirm-email?code={user.EmailConfirmationCode}",
            AcceptLink = $"/accept-invite?code={confirmationCode}",
            ResetPasswordLink = $"/reset-password?email={Uri.EscapeDataString(email)}",
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

    public string GenerateTemporaryPassword(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var password = new char[length];

        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            var buffer = new byte[length * 4];
            randomNumberGenerator.GetBytes(buffer);

            for (var index = 0; index < length; index++)
            {
                var randomValue = BitConverter.ToUInt32(buffer, index * 4);
                password[index] = chars[(int)(randomValue % (uint)chars.Length)];
            }
        }

        return new string(password);
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
            // For new users, include temporary password and confirmation link
            templateType = EmailTemplateType.WorkspaceInviteNewUser;
            variables["TEMPORARY_PASSWORD"] = invitationResult.TemporaryPassword;
            variables["CONFIRMATION_LINK"] = invitationResult.ConfirmationLink;
            variables["SET_PASSWORD_LINK"] = invitationResult.ResetPasswordLink;
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


