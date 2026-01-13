using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Implementation of IRoleManagementService.
/// Manages role assignments and role operations.
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public RoleManagementService(
        IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
        IRoleRepository roleRepository)
    {
        _userWorkspaceRoleRepository = userWorkspaceRoleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<UserWorkspaceRole> AssignRoleToUserAsync(int userId, int workspaceId, int roleId, int assignedByUserId)
    {
        // Verify role belongs to workspace
        var role = await _roleRepository.FindByIdAsync(roleId);
        if (role == null || role.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException($"Role {roleId} does not belong to workspace {workspaceId}.");
        }

        // Add the assignment
        await _userWorkspaceRoleRepository.AddAsync(userId, workspaceId, roleId, assignedByUserId);

        // Return the created assignment
        var assignment = new UserWorkspaceRole
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = assignedByUserId
        };

        return assignment;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int workspaceId, int roleId)
    {
        try
        {
            await _userWorkspaceRoleRepository.RemoveAsync(userId, workspaceId, roleId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> CountRoleAssignmentsAsync(int workspaceId, int roleId)
    {
        return await _userWorkspaceRoleRepository.CountAssignmentsForRoleAsync(workspaceId, roleId);
    }

    public async Task<bool> RoleBelongsToWorkspaceAsync(int roleId, int workspaceId)
    {
        var role = await _roleRepository.FindByIdAsync(roleId);
        return role != null && role.WorkspaceId == workspaceId;
    }

    public async Task<List<Role>> GetWorkspaceRolesAsync(int workspaceId)
    {
        return await _roleRepository.ListForWorkspaceAsync(workspaceId);
    }

    public async Task<List<Role>> GetUserRolesAsync(int userId, int workspaceId)
    {
        return await _userWorkspaceRoleRepository.GetRolesAsync(userId, workspaceId);
    }

    public async Task EnsureRoleCanBeDeletedAsync(int workspaceId, int roleId, string roleName)
    {
        var assignCount = await CountRoleAssignmentsAsync(workspaceId, roleId);
        if (assignCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete role '{roleName}' while {assignCount} user(s) are assigned. Unassign them first.");
        }
    }
}
