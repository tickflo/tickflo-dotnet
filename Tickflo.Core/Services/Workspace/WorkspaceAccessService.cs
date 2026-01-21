using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Workspace;

namespace Tickflo.Core.Services.Workspace;

/// <summary>
/// Implementation of IWorkspaceAccessService.
/// Provides workspace access verification and permission checking.
/// </summary>
public class WorkspaceAccessService : IWorkspaceAccessService
{
    #region Constants
    private const string ViewAction = "view";
    private const string CreateAction = "create";
    private const string EditAction = "edit";
    private const string AllTicketsScope = "all";
    private const string UserNotAdminErrorFormat = "User {0} is not an admin of workspace {1}.";
    private const string UserNoAccessErrorFormat = "User {0} does not have access to workspace {1}.";
    #endregion

    private readonly IUserWorkspaceRepository _userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public WorkspaceAccessService(
        IUserWorkspaceRepository userWorkspaceRepository,
        IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
        IRolePermissionRepository rolePermissionRepository)
    {
        _userWorkspaceRepository = userWorkspaceRepository;
        _userWorkspaceRoleRepository = userWorkspaceRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
    }

    public async Task<bool> UserHasAccessAsync(int userId, int workspaceId)
    {
        var userWorkspace = await _userWorkspaceRepository.FindAsync(userId, workspaceId);
        return userWorkspace?.Accepted ?? false;
    }

    public async Task<bool> UserIsWorkspaceAdminAsync(int userId, int workspaceId)
    {
        return await _userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
    }

    public async Task<Dictionary<string, EffectiveSectionPermission>> GetUserPermissionsAsync(int workspaceId, int userId)
    {
        var permissions = await _rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
        return permissions;
    }

    public async Task<bool> CanUserPerformActionAsync(int workspaceId, int userId, string resourceType, string action)
    {
        if (await UserIsWorkspaceAdminAsync(userId, workspaceId))
            return true;

        var permissions = await GetUserPermissionsAsync(workspaceId, userId);
        if (!permissions.TryGetValue(resourceType, out var permission))
            return false;

        return IsActionAllowed(permission, action);
    }

    public async Task<string> GetTicketViewScopeAsync(int workspaceId, int userId, bool isAdmin)
    {
        if (isAdmin)
            return AllTicketsScope;

        return await _rolePermissionRepository.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
    }

    public async Task EnsureAdminAccessAsync(int userId, int workspaceId)
    {
        var isAdmin = await UserIsWorkspaceAdminAsync(userId, workspaceId);
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException(string.Format(UserNotAdminErrorFormat, userId, workspaceId));
        }
    }

    public async Task EnsureWorkspaceAccessAsync(int userId, int workspaceId)
    {
        var hasAccess = await UserHasAccessAsync(userId, workspaceId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(string.Format(UserNoAccessErrorFormat, userId, workspaceId));
        }
    }

    private bool IsActionAllowed(EffectiveSectionPermission permission, string action)
    {
        return action switch
        {
            ViewAction => permission.CanView,
            CreateAction => permission.CanCreate,
            EditAction => permission.CanEdit,
            _ => false
        };
    }
}



