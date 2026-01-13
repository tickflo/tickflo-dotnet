using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Users;

/// <summary>
/// Handles user onboarding and workspace assignment workflows.
/// </summary>
public class UserOnboardingService : IUserOnboardingService
{
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _roleAssignmentRepo;
    private readonly IWorkspaceRepository _workspaceRepo;

    public UserOnboardingService(
        IUserRepository userRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IUserWorkspaceRoleRepository roleAssignmentRepo,
        IWorkspaceRepository workspaceRepo)
    {
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _roleAssignmentRepo = roleAssignmentRepo;
        _workspaceRepo = workspaceRepo;
    }

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
            throw new InvalidOperationException("Invalid email address format");

        var workspace = await _workspaceRepo.FindByIdAsync(workspaceId);
        if (workspace == null)
            throw new InvalidOperationException("Workspace not found");

        // Business rule: Find or create user
        var user = await _userRepo.FindByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException("User does not exist. They must register first.");
        }

        // Business rule: Check if user already has workspace access
        var existingAccess = await _userWorkspaceRepo.FindAsync(user.Id, workspaceId);
        if (existingAccess != null)
            throw new InvalidOperationException("User already has access to this workspace");

        // Create workspace assignment
        var assignment = new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspaceId,
            Accepted = false, // User must accept invitation
            CreatedAt = DateTime.UtcNow,
            CreatedBy = invitedByUserId
        };

        await _userWorkspaceRepo.AddAsync(assignment);

        // Assign default role if provided
        if (roleId > 0)
        {
            await _roleAssignmentRepo.AddAsync(user.Id, workspaceId, roleId, invitedByUserId);
        }

        // Business rule: Could send invitation email here

        return assignment;
    }

    /// <summary>
    /// Accepts a pending workspace invitation.
    /// </summary>
    public async Task<UserWorkspace> AcceptInvitationAsync(int userId, int workspaceId)
    {
        var assignment = await _userWorkspaceRepo.FindAsync(userId, workspaceId);
        if (assignment == null)
            throw new InvalidOperationException("Invitation not found");

        if (assignment.Accepted)
            return assignment; // Already accepted

        // Business rule: Mark invitation as accepted
        assignment.Accepted = true;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _userWorkspaceRepo.UpdateAsync(assignment);

        // Could add: Send welcome email, trigger onboarding workflows, etc.

        return assignment;
    }

    /// <summary>
    /// Declines a pending workspace invitation.
    /// </summary>
    public async Task DeclineInvitationAsync(int userId, int workspaceId)
    {
        var assignment = await _userWorkspaceRepo.FindAsync(userId, workspaceId);
        if (assignment == null)
            throw new InvalidOperationException("Invitation not found");

        if (assignment.Accepted)
            throw new InvalidOperationException("Cannot decline an accepted invitation");

        // Repository doesn't have delete - would need to be added
        throw new NotImplementedException("Invitation decline requires repository support");
    }

    /// <summary>
    /// Removes a user from a workspace.
    /// </summary>
    public async Task RemoveUserFromWorkspaceAsync(int userId, int workspaceId, int removedByUserId)
    {
        var assignment = await _userWorkspaceRepo.FindAsync(userId, workspaceId);
        if (assignment == null)
            throw new InvalidOperationException("User does not have access to this workspace");

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
