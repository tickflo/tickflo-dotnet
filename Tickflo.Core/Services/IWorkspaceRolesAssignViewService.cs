using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceRolesAssignViewData
{
    public bool IsAdmin { get; set; }
    public List<User> Members { get; set; } = new();
    public List<Role> Roles { get; set; } = new();
    public Dictionary<int, List<Role>> UserRoles { get; set; } = new();
}

public interface IWorkspaceRolesAssignViewService
{
    Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId);
}
