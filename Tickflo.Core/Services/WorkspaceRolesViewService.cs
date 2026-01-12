using Tickflo.Core.Data;

namespace Tickflo.Core.Services;

public class WorkspaceRolesViewService : IWorkspaceRolesViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IRoleManagementService _roleManagementService;

    public WorkspaceRolesViewService(
        IWorkspaceAccessService workspaceAccessService,
        IRoleManagementService roleManagementService)
    {
        _workspaceAccessService = workspaceAccessService;
        _roleManagementService = roleManagementService;
    }

    public async Task<WorkspaceRolesViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesViewData();

        // Check if user is admin (only admins can view roles)
        data.IsAdmin = await _workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId);

        // Only load roles if user is admin
        if (data.IsAdmin)
        {
            var roles = await _roleManagementService.GetWorkspaceRolesAsync(workspaceId);
            data.Roles = roles;

            // Count assignments for each role
            data.RoleAssignmentCounts = new Dictionary<int, int>();
            foreach (var role in roles)
            {
                data.RoleAssignmentCounts[role.Id] = await _roleManagementService.CountRoleAssignmentsAsync(workspaceId, role.Id);
            }
        }

        return data;
    }
}
