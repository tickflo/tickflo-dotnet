namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Workspace;

public interface IWorkspaceRolesViewService
{
    public Task<WorkspaceRolesViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceRolesViewData
{
    public List<Role> Roles { get; set; } = [];
    public Dictionary<int, int> RoleAssignmentCounts { get; set; } = [];
    public bool IsAdmin { get; set; }
}


public class WorkspaceRolesViewService(
    IWorkspaceAccessService workspaceAccessService,
    IRoleManagementService roleManagementService) : IWorkspaceRolesViewService
{
    private readonly IWorkspaceAccessService workspaceAccessService = workspaceAccessService;
    private readonly IRoleManagementService roleManagementService = roleManagementService;

    public async Task<WorkspaceRolesViewData> BuildAsync(int workspaceId, int userId)
    {
        var data = new WorkspaceRolesViewData
        {
            // Check if user is admin (only admins can view roles)
            IsAdmin = await this.workspaceAccessService.UserIsWorkspaceAdminAsync(userId, workspaceId)
        };

        // Only load roles if user is admin
        if (data.IsAdmin)
        {
            var roles = await this.roleManagementService.GetWorkspaceRolesAsync(workspaceId);
            data.Roles = roles;

            // Count assignments for each role
            data.RoleAssignmentCounts = [];
            foreach (var role in roles)
            {
                data.RoleAssignmentCounts[role.Id] = await this.roleManagementService.CountRoleAssignmentsAsync(workspaceId, role.Id);
            }
        }

        return data;
    }
}



