using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public interface IWorkspaceRolesViewService
{
    Task<WorkspaceRolesViewData> BuildAsync(int workspaceId, int userId);
}

public class WorkspaceRolesViewData
{
    public List<Role> Roles { get; set; } = new();
    public Dictionary<int, int> RoleAssignmentCounts { get; set; } = new();
    public bool IsAdmin { get; set; }
}


