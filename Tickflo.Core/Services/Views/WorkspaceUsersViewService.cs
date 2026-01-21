using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

/// <summary>
/// Implementation of workspace users view service.
/// </summary>
public class WorkspaceUsersViewService : IWorkspaceUsersViewService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserRepository _userRepo;

    public WorkspaceUsersViewService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRolePermissionRepository rolePerms,
        IUserWorkspaceRepository userWorkspaceRepo,
        IUserRepository userRepo)
    {
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userRepo = userRepo;
    }

    public async Task<WorkspaceUsersViewData> BuildAsync(int workspaceId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var data = new WorkspaceUsersViewData();

        // Determine admin and permissions
        data.IsWorkspaceAdmin = currentUserId > 0 && await _userWorkspaceRoleRepo.IsAdminAsync(currentUserId, workspaceId);
        if (currentUserId > 0)
        {
            var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, currentUserId);
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
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        foreach (var m in memberships.Where(m => !m.Accepted))
        {
            var u = await _userRepo.FindByIdAsync(m.UserId);
            if (u == null) continue;
            var roles = await _userWorkspaceRoleRepo.GetRoleNamesAsync(u.Id, workspaceId);
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
            var u = await _userRepo.FindByIdAsync(m.UserId);
            if (u == null) continue;
            var roles = await _userWorkspaceRoleRepo.GetRoleNamesAsync(u.Id, workspaceId);
            var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(u.Id, workspaceId);
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


