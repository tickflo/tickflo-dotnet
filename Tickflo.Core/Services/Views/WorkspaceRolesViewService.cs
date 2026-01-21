namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Workspace;

public class WorkspaceRolesViewService(
    IWorkspaceAccessService workspaceAccessService,
    IRoleManagementService roleManagementService) : IWorkspaceRolesViewService
{
    private readonly IWorkspaceAccessService _workspaceAccessService = workspaceAccessService;
    private readonly IRoleManagementService _roleManagementService = roleManagementService;

    public async Task<WorkspaceRolesViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesViewData
        {
            // Check if user is admin (only admins can view roles)
            IsAdmin = await this._workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId)
        };

        // Only load roles if user is admin
        if (data.IsAdmin)
        {
            var roles = await this._roleManagementService.GetWorkspaceRolesAsync(workspaceId);
            data.Roles = roles;

            // Count assignments for each role
            data.RoleAssignmentCounts = [];
            foreach (var role in roles)
            {
                data.RoleAssignmentCounts[role.Id] = await this._roleManagementService.CountRoleAssignmentsAsync(workspaceId, role.Id);
            }
        }

        return data;
    }
}



