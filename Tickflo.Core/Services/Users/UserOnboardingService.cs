namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles user onboarding and workspace assignment workflows.
/// </summary>
public class UserOnboardingService(
    IUserRepository userRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserWorkspaceRoleRepository roleAssignmentRepo,
    IWorkspaceRepository workspaceRepo) : IUserOnboardingService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository _roleAssignmentRepo = roleAssignmentRepo;
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;

    /// <summary>
    /// Invites a user to a workspace with a specific role.
    /// </summary>
    public async Task<UserWorkspace> InviteUserToWorkspaceAsync(
        int workspaceId,
        string email,
        int roleId,
        int invitedByUserId)
    {
        // Business rule: Validate email format
        if (!IsValidEmail(email))
        {
            throw new InvalidOperationException("Invalid email address format");
        }

        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId) ?? throw new InvalidOperationException("Workspace not found");

        // Business rule: Find or create user
        var user = await this.userRepository.FindByEmailAsync(email) ?? throw new InvalidOperationException("User does not exist. They must register first.");

        // Business rule: Check if user already has workspace access
        var existingAccess = await this.userWorkspaceRepository.FindAsync(user.Id, workspaceId);
        if (existingAccess != null)
        {
            throw new InvalidOperationException("User already has access to this workspace");
        }

        // Create workspace assignment
        var assignment = new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspaceId,
            Accepted = false, // User must accept invitation
            CreatedAt = DateTime.UtcNow,
            CreatedBy = invitedByUserId
        };

        await this.userWorkspaceRepository.AddAsync(assignment);

        // Assign default role if provided
        if (roleId > 0)
        {
            await this._roleAssignmentRepo.AddAsync(user.Id, workspaceId, roleId, invitedByUserId);
        }

        // Business rule: Could send invitation email here

        return assignment;
    }

    /// <summary>
    /// Accepts a pending workspace invitation.
    /// </summary>
    public async Task<UserWorkspace> AcceptInvitationAsync(int userId, int workspaceId)
    {
        var assignment = await this.userWorkspaceRepository.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("Invitation not found");

        if (assignment.Accepted)
        {
            return assignment; // Already accepted
        }

        // Business rule: Mark invitation as accepted
        assignment.Accepted = true;
        assignment.UpdatedAt = DateTime.UtcNow;

        await this.userWorkspaceRepository.UpdateAsync(assignment);

        // Could add: Send welcome email, trigger onboarding workflows, etc.

        return assignment;
    }

    /// <summary>
    /// Declines a pending workspace invitation.
    /// </summary>
    public async Task DeclineInvitationAsync(int userId, int workspaceId)
    {
        var assignment = await this.userWorkspaceRepository.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("Invitation not found");

        if (assignment.Accepted)
        {
            throw new InvalidOperationException("Cannot decline an accepted invitation");
        }

        // Repository doesn't have delete - would need to be added
        throw new NotImplementedException("Invitation decline requires repository support");
    }

    /// <summary>
    /// Removes a user from a workspace.
    /// </summary>
    public async Task RemoveUserFromWorkspaceAsync(int userId, int workspaceId, int removedByUserId)
    {
        var assignment = await this.userWorkspaceRepository.FindAsync(userId, workspaceId) ?? throw new InvalidOperationException("User does not have access to this workspace");

        // Business rule: Could prevent removal of last admin
        // Business rule: Could reassign tickets, etc.
        // Note: Repository doesn't have delete - this would need to be added or handled differently
        // For now, we'll mark as a business rule that needs repo support
        throw new NotImplementedException("User workspace removal requires repository support");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
