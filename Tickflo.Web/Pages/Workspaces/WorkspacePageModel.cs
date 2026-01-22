namespace Tickflo.Web.Pages.Workspaces;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

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
        var idValue = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
        if (!this.TryGetUserId(out userId))
        {
            userId = 0;
            return this.NotFound();
        }
        return null;
    }

    /// <summary>
    /// Checks if a workspace is null and returns NotFound if it is.
    /// </summary>
    /// <param name="workspace">The workspace to check</param>
    /// <returns>NotFoundResult if workspace is null, null otherwise</returns>
    protected IActionResult? EnsureWorkspaceExistsOrNotFound(Workspace? workspace) => workspace == null ? this.NotFound() : null;

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
            return this.NotFound();
        }
        return null;
    }

    /// <summary>
    /// Checks if an entity is null and returns NotFound if it is.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entity">The entity to check</param>
    /// <returns>NotFoundResult if entity is null, null otherwise</returns>
    protected IActionResult? EnsureEntityExistsOrNotFound<T>(T? entity) where T : class => entity == null ? this.NotFound() : null;

    /// <summary>
    /// Checks if a permission is granted and returns Forbid if not.
    /// </summary>
    /// <param name="hasPermission">Whether the user has the required permission</param>
    /// <returns>ForbidResult if permission is denied, null otherwise</returns>
    protected IActionResult? EnsurePermissionOrForbid(bool hasPermission) => hasPermission ? null : this.Forbid();

    /// <summary>
    /// Checks create or edit permissions based on entity ID (0 = create, > 0 = edit).
    /// </summary>
    /// <param name="entityId">The entity ID (0 for new, > 0 for existing)</param>
    /// <param name="canCreate">Whether the user has create permission</param>
    /// <param name="canEdit">Whether the user has edit permission</param>
    /// <returns>ForbidResult if permission is denied, null otherwise</returns>
    protected IActionResult? EnsureCreateOrEditPermission(int entityId, bool canCreate, bool canEdit)
    {
        if (entityId == 0 && !canCreate)
        {
            return this.Forbid();
        }

        if (entityId > 0 && !canEdit)
        {
            return this.Forbid();
        }

        return null;
    }

    /// <summary>
    /// Sets a success message in TempData with standard key.
    /// </summary>
    /// <param name="message">The success message to display</param>
    protected void SetSuccessMessage(string message) => this.TempData["Success"] = message;

    /// <summary>
    /// Sets an error message in TempData with standard key.
    /// </summary>
    /// <param name="message">The error message to display</param>
    protected void SetErrorMessage(string message) => this.TempData["Error"] = message;

    /// <summary>
    /// Loads a workspace by slug and validates the current user.
    /// Returns NotFound if workspace doesn't exist, or Forbid if user ID cannot be extracted.
    /// </summary>
    /// <param name="workspaceRepository">The workspace repository</param>
    /// <param name="slug">The workspace slug</param>
    /// <returns>WorkspaceUserLoadResult on success, or IActionResult (NotFound/Forbid) on failure</returns>
    protected async Task<object> LoadWorkspaceAndUserOrExitAsync(IWorkspaceRepository workspaceRepository, string slug)
    {
        var workspace = await workspaceRepository.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        return new WorkspaceUserLoadResult(workspace, userId);
    }

    // TODO: This should probably just throw an exception or return a different wrapped type.. Returning object is gross.
    /// <summary>
    /// Loads a workspace by slug and validates both that it exists and that the current user is a member.
    /// Returns NotFound if workspace doesn't exist, Forbid if user is not a member or cannot be identified.
    /// </summary>
    /// <param name="workspaceRepository">The workspace repository</param>
    /// <param name="userWorkspaceRepository">The user workspace repository for membership validation</param>
    /// <param name="slug">The workspace slug</param>
    /// <returns>WorkspaceUserLoadResult on success, or IActionResult (NotFound/Forbid) on failure</returns>
    protected async Task<object> LoadWorkspaceAndValidateUserMembershipAsync(
        IWorkspaceRepository workspaceRepository,
        IUserWorkspaceRepository userWorkspaceRepository,
        string slug)
    {
        var workspace = await workspaceRepository.FindBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        // Validate that the user is a member of this workspace
        var membership = await userWorkspaceRepository.FindAsync(userId, workspace.Id);
        if (membership == null)
        {
            return this.Forbid();
        }

        return new WorkspaceUserLoadResult(workspace, userId);
    }
}
