using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

/// <summary>
/// Result from workspace and user validation operations.
/// </summary>
public record WorkspaceUserLoadResult(Workspace? Workspace, int UserId);

/// <summary>
/// Base class for all workspace pages.
/// Provides common functionality like user ID extraction and workspace loading.
/// </summary>
public abstract class WorkspacePageModel : PageModel
{
    /// <summary>
    /// Safely extracts the current user ID from claims.
    /// </summary>
    /// <param name="userId">The extracted user ID, or 0 if not found</param>
    /// <returns>True if user ID was successfully extracted, false otherwise</returns>
    protected bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }

    /// <summary>
    /// Checks if the current user ID can be extracted and returns NotFound if unable.
    /// </summary>
    /// <param name="userId">The extracted user ID</param>
    /// <returns>NotFoundResult if user ID cannot be extracted, null otherwise</returns>
    protected IActionResult? EnsureUserIdOrNotFound(out int userId)
    {
        if (!TryGetUserId(out userId))
        {
            userId = 0;
            return NotFound();
        }
        return null;
    }

    /// <summary>
    /// Checks if a workspace is null and returns NotFound if it is.
    /// </summary>
    /// <param name="workspace">The workspace to check</param>
    /// <returns>NotFoundResult if workspace is null, null otherwise</returns>
    protected IActionResult? EnsureWorkspaceExistsOrNotFound(Workspace? workspace)
    {
        return workspace == null ? NotFound() : null;
    }

    /// <summary>
    /// Checks if an entity is null or belongs to a different workspace, returning NotFound if either condition is true.
    /// </summary>
    /// <typeparam name="T">The entity type that has a WorkspaceId property</typeparam>
    /// <param name="entity">The entity to check</param>
    /// <param name="expectedWorkspaceId">The expected workspace ID</param>
    /// <returns>NotFoundResult if entity is null or doesn't belong to the workspace, null otherwise</returns>
    protected IActionResult? EnsureEntityBelongsToWorkspace<T>(T? entity, int expectedWorkspaceId) where T : class, IWorkspaceEntity
    {
        if (entity == null || entity.WorkspaceId != expectedWorkspaceId)
        {
            return NotFound();
        }
        return null;
    }

    /// <summary>
    /// Checks if an entity is null and returns NotFound if it is.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entity">The entity to check</param>
    /// <returns>NotFoundResult if entity is null, null otherwise</returns>
    protected IActionResult? EnsureEntityExistsOrNotFound<T>(T? entity) where T : class
    {
        return entity == null ? NotFound() : null;
    }

    /// <summary>
    /// Checks if a permission is granted and returns Forbid if not.
    /// </summary>
    /// <param name="hasPermission">Whether the user has the required permission</param>
    /// <returns>ForbidResult if permission is denied, null otherwise</returns>
    protected IActionResult? EnsurePermissionOrForbid(bool hasPermission)
    {
        return hasPermission ? null : Forbid();
    }

    /// <summary>
    /// Checks create or edit permissions based on entity ID (0 = create, > 0 = edit).
    /// </summary>
    /// <param name="entityId">The entity ID (0 for new, > 0 for existing)</param>
    /// <param name="canCreate">Whether the user has create permission</param>
    /// <param name="canEdit">Whether the user has edit permission</param>
    /// <returns>ForbidResult if permission is denied, null otherwise</returns>
    protected IActionResult? EnsureCreateOrEditPermission(int entityId, bool canCreate, bool canEdit)
    {
        if (entityId == 0 && !canCreate) return Forbid();
        if (entityId > 0 && !canEdit) return Forbid();
        return null;
    }

    /// <summary>
    /// Sets a success message in TempData with standard key.
    /// </summary>
    /// <param name="message">The success message to display</param>
    protected void SetSuccessMessage(string message)
    {
        TempData["Success"] = message;
    }

    /// <summary>
    /// Sets an error message in TempData with standard key.
    /// </summary>
    /// <param name="message">The error message to display</param>
    protected void SetErrorMessage(string message)
    {
        TempData["Error"] = message;
    }

    /// <summary>
    /// Loads a workspace by slug and validates the current user.
    /// Returns NotFound if workspace doesn't exist, or Forbid if user ID cannot be extracted.
    /// </summary>
    /// <param name="workspaceRepo">The workspace repository</param>
    /// <param name="slug">The workspace slug</param>
    /// <returns>WorkspaceUserLoadResult on success, or IActionResult (NotFound/Forbid) on failure</returns>
    protected async Task<object> LoadWorkspaceAndUserOrExitAsync(IWorkspaceRepository workspaceRepo, string slug)
    {
        var workspace = await workspaceRepo.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return NotFound();
        }

        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        return new WorkspaceUserLoadResult(workspace, userId);
    }

    /// <summary>
    /// Loads a workspace by slug and validates both that it exists and that the current user is a member.
    /// Returns NotFound if workspace doesn't exist, Forbid if user is not a member or cannot be identified.
    /// </summary>
    /// <param name="workspaceRepo">The workspace repository</param>
    /// <param name="userWorkspaceRepo">The user workspace repository for membership validation</param>
    /// <param name="slug">The workspace slug</param>
    /// <returns>WorkspaceUserLoadResult on success, or IActionResult (NotFound/Forbid) on failure</returns>
    protected async Task<object> LoadWorkspaceAndValidateUserMembershipAsync(
        IWorkspaceRepository workspaceRepo, 
        IUserWorkspaceRepository userWorkspaceRepo, 
        string slug)
    {
        var workspace = await workspaceRepo.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return NotFound();
        }

        if (!TryGetUserId(out var userId))
        {
            return Forbid();
        }

        // Validate that the user is a member of this workspace
        var membership = await userWorkspaceRepo.FindAsync(userId, workspace.Id);
        if (membership == null)
        {
            return Forbid();
        }

        return new WorkspaceUserLoadResult(workspace, userId);
    }
}
