namespace Tickflo.Core.Services.Workspace;

using System.Text;
using Tickflo.Core.Data;

/// <summary>
/// Implementation of IWorkspaceAccessService.
/// Provides workspace access verification and permission checking.
/// </summary>
public class WorkspaceAccessService(
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
    IRolePermissionRepository rolePermissionRepository) : IWorkspaceAccessService
{
    #region Constants
    private const string ViewAction = "view";
    private const string CreateAction = "create";
    private const string EditAction = "edit";
    private const string AllTicketsScope = "all";
    private static readonly CompositeFormat UserNotAdminErrorFormat = CompositeFormat.Parse("User {0} is not an admin of workspace {1}.");
    private static readonly CompositeFormat UserNoAccessErrorFormat = CompositeFormat.Parse("User {0} does not have access to workspace {1}.");
    #endregion

    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;

    public async Task<bool> UserHasAccessAsync(int userId, int workspaceId)
    {
        var userWorkspace = await this.userWorkspaceRepository.FindAsync(userId, workspaceId);
        return userWorkspace?.Accepted ?? false;
    }

    public async Task<bool> UserIsWorkspaceAdminAsync(int userId, int workspaceId) => await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);

    public async Task<Dictionary<string, EffectiveSectionPermission>> GetUserPermissionsAsync(int workspaceId, int userId)
    {
        var permissions = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        return permissions;
    }

    public async Task<bool> CanUserPerformActionAsync(int workspaceId, int userId, string resourceType, string action)
    {
        if (await this.UserIsWorkspaceAdminAsync(userId, workspaceId))
        {
            return true;
        }

        var permissions = await this.GetUserPermissionsAsync(workspaceId, userId);
        if (!permissions.TryGetValue(resourceType, out var permission))
        {
            return false;
        }

        return IsActionAllowed(permission, action);
    }

    public async Task<string> GetTicketViewScopeAsync(int workspaceId, int userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return AllTicketsScope;
        }

        return await this.rolePermissionRepository.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
    }

    public async Task EnsureAdminAccessAsync(int userId, int workspaceId)
    {
        var isAdmin = await this.UserIsWorkspaceAdminAsync(userId, workspaceId);
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException(string.Format(null, UserNotAdminErrorFormat, userId, workspaceId));
        }
    }

    public async Task EnsureWorkspaceAccessAsync(int userId, int workspaceId)
    {
        var hasAccess = await this.UserHasAccessAsync(userId, workspaceId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(string.Format(null, UserNoAccessErrorFormat, userId, workspaceId));
        }
    }

    private static bool IsActionAllowed(EffectiveSectionPermission permission, string action) => action switch
    {
        ViewAction => permission.CanView,
        CreateAction => permission.CanCreate,
        EditAction => permission.CanEdit,
        _ => false
    };
}



