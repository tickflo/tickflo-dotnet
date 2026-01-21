namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class WorkspaceRolesEditViewData
{
    public bool IsAdmin { get; set; }
    public Role? ExistingRole { get; set; }
    public List<EffectiveSectionPermission> ExistingPermissions { get; set; } = [];
}

public interface IWorkspaceRolesEditViewService
{
    public Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0);
}


