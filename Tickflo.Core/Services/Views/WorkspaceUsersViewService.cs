namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;

/// <summary>
/// Implementation of workspace users view service.
/// </summary>
public class WorkspaceUsersViewService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRolePermissionRepository rolePerms,
    IUserWorkspaceRepository userWorkspaceRepo,
    IUserRepository userRepo) : IWorkspaceUsersViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms = rolePerms;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IUserRepository _userRepo = userRepo;

    public async Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var data = new WorkspaceUsersViewData
        {
            // Determine admin and permissions
            IsWorkspaceAdmin = currentUserId > 0 && await this._userWorkspaceRoleRepo.IsAdminAsync(currentUserId, workspaceId)
        };
        if (currentUserId > 0)
        {
            var eff = await this._rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, currentUserId);
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
        var memberships = await this._userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        foreach (var m in memberships.Where(m => !m.Accepted))
        {
            var u = await this._userRepo.FindByIdAsync(m.UserId);
            if (u == null)
            {
                continue;
            }

            var roles = await this._userWorkspaceRoleRepo.GetRoleNamesAsync(u.Id, workspaceId);
            data.PendingInvites.Add(new InviteView
            {
                UserId = u.Id,
                Email = u.Email,
                CreatedAt = m.CreatedAt,
                Roles = roles
            });
        }

        // Build accepted users
        foreach (var m in memberships.Where(m => m.Accepted))
        {
            var u = await this._userRepo.FindByIdAsync(m.UserId);
            if (u == null)
            {
                continue;
            }

            var roles = await this._userWorkspaceRoleRepo.GetRoleNamesAsync(u.Id, workspaceId);
            var isAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(u.Id, workspaceId);
            data.AcceptedUsers.Add(new AcceptedUserView
            {
                UserId = u.Id,
                Email = u.Email,
                Name = u.Name ?? string.Empty,
                JoinedAt = m.UpdatedAt ?? m.CreatedAt,
                Roles = roles,
                IsAdmin = isAdmin
            });
        }

        return data;
    }
}


