namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
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
    IEmailSendService emailSendService,
    IWorkspaceRepository workspaceRepository,
    TickfloConfig config) : IUserInvitationService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly TickfloConfig config = config;

    public async Task InviteUserAsync(
        int workspaceId,
        string email,
        int invitedByUserId,
        List<int> roleIds)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required");
        }

        if (roleIds.Count == 0)
        {
            throw new InvalidOperationException("At least one role is required");
        }

        email = email.Trim().ToLowerInvariant();

        // Get workspace for email template
        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId) ?? throw new InvalidOperationException("Workspace not found");

        // Check if user already exists
        var user = await this.userRepository.FindByEmailAsync(email);
        var isNewUser = user == null;

        if (isNewUser)
        {
            user = new User
            {
                Name = email.Split('@')[0],
                Email = email,
                EmailConfirmationCode = SecureTokenGenerator.GenerateToken(16),
            };

            await this.userRepository.AddAsync(user);
        }
        else
        {
            var existingMembership = await this.userWorkspaceRepository.FindAsync(user!.Id, workspaceId);
            if (existingMembership != null)
            {
                throw new InvalidOperationException("User is already invited to this workspace");
            }
        }

        if (user == null)
        {
            throw new InvalidOperationException("Failed to create or retrieve user");
        }

        await this.userWorkspaceRepository.AddAsync(new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspaceId,
            CreatedBy = invitedByUserId,
        });

        foreach (var roleId in roleIds)
        {
            var role = await this.roleRepository.FindByIdAsync(roleId);
            if (role == null || role.WorkspaceId != workspaceId)
            {
                throw new InvalidOperationException($"Role with ID {roleId} not found in this workspace");
            }

            await this.userWorkspaceRoleRepository.AddAsync(new UserWorkspaceRole
            {
                UserId = user.Id,
                WorkspaceId = workspaceId,
                RoleId = roleId,
                CreatedBy = invitedByUserId,
            });
        }

        if (isNewUser)
        {
            await this.SendNewUserInvitationEmailAsync(workspace, user, invitedByUserId);
        }
        else
        {
            await this.SendExistingUserInvitationEmailAsync(workspace, user, invitedByUserId);
        }
    }

    public async Task ResendInvitationAsync(int workspaceId, int userId, int resentByUserId)
    {
        var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId)
            ?? throw new InvalidOperationException("Workspace not found");

        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspaceId)
            ?? throw new InvalidOperationException("User is not invited to this workspace");

        var user = await this.userRepository.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (user.PasswordHash == null)
        {
            await this.SendNewUserInvitationEmailAsync(
                workspace,
                user,
                resentByUserId);
        }
        else
        {
            await this.SendExistingUserInvitationEmailAsync(
                workspace,
                user,
                resentByUserId);
        }
    }

    public async Task AcceptInvitationAsync(string slug, int userId)
    {
        var workspace = await this.workspaceRepository.FindBySlugAsync(slug)
            ?? throw new NotFoundException("Workspace not found");

        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspace.Id)
            ?? throw new InvalidOperationException("Invitation not found");

        if (membership.Accepted)
        {
            return;
        }

        membership.Accepted = true;
        membership.UpdatedAt = DateTime.UtcNow;
        membership.UpdatedBy = userId;
        await this.userWorkspaceRepository.UpdateAsync(membership);
    }

    public async Task DeclineInvitationAsync(string slug, int userId)
    {
        var workspace = await this.workspaceRepository.FindBySlugAsync(slug)
            ?? throw new NotFoundException("Workspace not found");

        var membership = await this.userWorkspaceRepository.FindAsync(userId, workspace.Id)
            ?? throw new InvalidOperationException("Invitation not found");

        if (membership.Accepted)
        {
            throw new InvalidOperationException("Cannot decline an accepted invitation");
        }

        await this.userWorkspaceRepository.DeleteAsync(membership);
    }

    private async Task SendNewUserInvitationEmailAsync(
        Workspace workspace,
        User user,
        int invitedByUserId
    )
    {
        var variables = new Dictionary<string, string>
        {
            { "name", user.Name },
            { "workspace_name", workspace.Name },
            { "signup_link", $"{this.config.BaseUrl}/signup?email={Uri.EscapeDataString(user.Email)}" },
        };

        await this.emailSendService.AddToQueueAsync(
            user.Email,
            EmailTemplateType.WorkspaceInviteNewUser,
            variables,
            invitedByUserId
        );
    }

    private async Task SendExistingUserInvitationEmailAsync(
        Workspace workspace,
        User user,
        int invitedByUserId
    )
    {
        var variables = new Dictionary<string, string>
        {
            { "name", user.Name },
            { "workspace_name", workspace.Name },
            { "login_link", $"{this.config.BaseUrl}/workspaces" },
        };

        await this.emailSendService.AddToQueueAsync(
            user.Email,
            EmailTemplateType.WorkspaceInviteExistingUser,
            variables,
            invitedByUserId
        );
    }
}


