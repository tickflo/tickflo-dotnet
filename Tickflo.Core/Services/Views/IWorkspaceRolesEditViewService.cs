using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

public class WorkspaceRolesEditViewData
{
    public bool IsAdmin { get; set; }
    public Role? ExistingRole { get; set; }
    public List<EffectiveSectionPermission> ExistingPermissions { get; set; } = new();
}

public interface IWorkspaceRolesEditViewService
{
    Task<WorkspaceRolesEditViewData> BuildAsync(int workspaceId, int userId, int roleId = 0);
}


