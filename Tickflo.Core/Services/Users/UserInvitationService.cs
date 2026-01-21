namespace Tickflo.Core.Services.Users;

using System.Security.Cryptography;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;

/// <summary>
/// Service for managing user invitations and onboarding workflows.
/// </summary>
public class UserInvitationService(
    IUserRepository userRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    IUserWorkspaceRoleRepository roleRepo,
    IRoleRepository rolesRepo,
    IPasswordHasher passwordHasher) : IUserInvitationService
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _roleRepo = roleRepo;
    private readonly IRoleRepository _rolesRepo = rolesRepo;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<UserInvitationResult> InviteUserAsync(
        int workspaceId,
        string email,
        int invitedByUserId,
        List<int>? roleIds = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required");
        }

        email = email.Trim().ToLowerInvariant();

        // Generate temporary password
        var tempPassword = this.GenerateTemporaryPassword(12);

        // Check if user already exists
        var existingUser = await this._userRepo.FindByEmailAsync(email);
        User user;

        if (existingUser == null)
        {
            // Create new user
            user = new User
            {
                Name = email.Split('@')[0], // Default name from email
                Email = email,
                PasswordHash = this._passwordHasher.Hash(tempPassword),
                EmailConfirmed = false,
                SystemAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            await this._userRepo.AddAsync(user);
        }
        else
        {
            user = existingUser;
        }

        // Generate confirmation code (using Guid for now)
        var confirmationCode = GenerateConfirmationCode();

        // Create or update workspace membership
        var existingMembership = await this._userWorkspaceRepo.FindAsync(user.Id, workspaceId);

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

            await this._userWorkspaceRepo.AddAsync(membership);
        }

        // Assign roles if provided
        if (roleIds != null && roleIds.Count > 0)
        {
            foreach (var roleId in roleIds)
            {
                // Verify role exists and belongs to workspace
                var role = await this._rolesRepo.FindByIdAsync(roleId);
                if (role != null && role.WorkspaceId == workspaceId)
                {
                    // Add role using repository method
                    await this._roleRepo.AddAsync(user.Id, workspaceId, roleId, invitedByUserId);
                }
            }
        }

        // Build invitation result
        var result = new UserInvitationResult
        {
            User = user,
            ConfirmationCode = confirmationCode,
            TemporaryPassword = tempPassword,
            ConfirmationLink = $"/confirm-email?code={confirmationCode}",
            AcceptLink = $"/accept-invite?code={confirmationCode}",
            ResetPasswordLink = $"/reset-password?email={Uri.EscapeDataString(email)}"
        };

        return result;
    }

    public async Task<string> ResendInvitationAsync(int workspaceId, int userId, int resentByUserId)
    {
        var membership = await this._userWorkspaceRepo.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("User is not invited to this workspace");

        // Generate new confirmation code
        var confirmationCode = GenerateConfirmationCode();

        // Note: ConfirmationCode is not stored in UserWorkspace entity currently
        // This would need to be added to the entity and database schema
        // For now, return the new code for the caller to handle

        return confirmationCode;
    }

    public async Task AcceptInvitationAsync(int workspaceId, int userId)
    {
        var membership = await this._userWorkspaceRepo.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("Invitation not found");

        membership.Accepted = true;
        await this._userWorkspaceRepo.UpdateAsync(membership);
    }

    public string GenerateTemporaryPassword(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var password = new char[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[length * 4];
            rng.GetBytes(buffer);

            for (var i = 0; i < length; i++)
            {
                var randomValue = BitConverter.ToUInt32(buffer, i * 4);
                password[i] = chars[(int)(randomValue % (uint)chars.Length)];
            }
        }

        return new string(password);
    }

    private static string GenerateConfirmationCode() => Guid.NewGuid().ToString("N");
}



