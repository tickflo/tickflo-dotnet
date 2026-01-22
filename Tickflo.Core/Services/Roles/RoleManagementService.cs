namespace Tickflo.Core.Services.Roles;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of IRoleManagementService.
/// Manages role assignments and role operations.
/// </summary>
public class RoleManagementService(
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
    IRoleRepository roleRepository) : IRoleManagementService
{
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
    private readonly IRoleRepository roleRepository = roleRepository;

    public async Task<UserWorkspaceRole> AssignRoleToUserAsync(int userId, int workspaceId, int roleId, int assignedByUserId)
    {
        // Verify role belongs to workspace
        var role = await this.roleRepository.FindByIdAsync(roleId);
        if (role == null || role.WorkspaceId != workspaceId)
        {
            throw new InvalidOperationException($"Role {roleId} does not belong to workspace {workspaceId}.");
        }

        // Add the assignment
        await this.userWorkspaceRoleRepository.AddAsync(userId, workspaceId, roleId, assignedByUserId);

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
            await this.userWorkspaceRoleRepository.RemoveAsync(userId, workspaceId, roleId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> CountRoleAssignmentsAsync(int workspaceId, int roleId) => await this.userWorkspaceRoleRepository.CountAssignmentsForRoleAsync(workspaceId, roleId);

    public async Task<bool> RoleBelongsToWorkspaceAsync(int roleId, int workspaceId)
    {
        var role = await this.roleRepository.FindByIdAsync(roleId);
        return role != null && role.WorkspaceId == workspaceId;
    }

    public async Task<List<Role>> GetWorkspaceRolesAsync(int workspaceId) => await this.roleRepository.ListForWorkspaceAsync(workspaceId);

    public async Task<List<Role>> GetUserRolesAsync(int userId, int workspaceId) => await this.userWorkspaceRoleRepository.GetRolesAsync(userId, workspaceId);

    public async Task EnsureRoleCanBeDeletedAsync(int workspaceId, int roleId, string roleName)
    {
        var assignCount = await this.CountRoleAssignmentsAsync(workspaceId, roleId);
        if (assignCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete role '{roleName}' while {assignCount} user(s) are assigned. Unassign them first.");
        }
    }
}


