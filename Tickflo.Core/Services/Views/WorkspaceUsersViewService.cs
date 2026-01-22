namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

/// <summary>
/// Implementation of workspace users view service.
/// </summary>
public class WorkspaceUsersViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePermissionRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserRepository userRepository) : IWorkspaceUsersViewService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserRepository userRepository = userRepository;

    public async Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var data = new WorkspaceUsersViewData
        {
            // Determine admin and permissions
            IsWorkspaceAdmin = currentUserId > 0 && await this.userWorkspaceRoleRepository.IsAdminAsync(currentUserId, workspaceId)
        };
        if (currentUserId > 0)
        {
            var eff = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, currentUserId);
            if (eff.TryGetValue("users", out var up))
            {
                data.CanViewUsers = up.CanView || data.IsWorkspaceAdmin;
                data.CanCreateUsers = up.CanCreate || data.IsWorkspaceAdmin;
                data.CanEditUsers = up.CanEdit || data.IsWorkspaceAdmin;
            }
            else
            {
                data.CanViewUsers = data.IsWorkspaceAdmin;
                data.CanCreateUsers = data.IsWorkspaceAdmin;
                data.CanEditUsers = data.IsWorkspaceAdmin;
            }
        }

        // Build pending invites
        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        foreach (var m in memberships.Where(m => !m.Accepted))
        {
            var user = await this.userRepository.FindByIdAsync(m.UserId);
            if (user == null)
            {
                continue;
            }

            var roles = await this.userWorkspaceRoleRepository.GetRoleNamesAsync(user.Id, workspaceId);
            data.PendingInvites.Add(new InviteView
            {
                UserId = user.Id,
                Email = user.Email,
                CreatedAt = m.CreatedAt,
                Roles = roles
            });
        }

        // Build accepted users
        foreach (var m in memberships.Where(m => m.Accepted))
        {
            var user = await this.userRepository.FindByIdAsync(m.UserId);
            if (user == null)
            {
                continue;
            }

            var roles = await this.userWorkspaceRoleRepository.GetRoleNamesAsync(user.Id, workspaceId);
            var isAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(user.Id, workspaceId);
            data.AcceptedUsers.Add(new AcceptedUserView
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name ?? string.Empty,
                JoinedAt = m.UpdatedAt ?? m.CreatedAt,
                Roles = roles,
                IsAdmin = isAdmin
            });
        }

        return data;
    }
}


