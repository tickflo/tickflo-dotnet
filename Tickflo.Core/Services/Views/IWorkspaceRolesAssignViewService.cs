namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Entities;

public class WorkspaceRolesAssignViewData
{
    public bool IsAdmin { get; set; }
    public List<User> Members { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
    public Dictionary<int, List<Role>> UserRoles { get; set; } = [];
}

public interface IWorkspaceRolesAssignViewService
{
    public Task<WorkspaceRolesAssignViewData> BuildAsync(int workspaceId, int userId);
}


