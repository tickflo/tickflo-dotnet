namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

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


