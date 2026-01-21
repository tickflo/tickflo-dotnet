namespace Tickflo.Core.Services.Locations;

using Tickflo.Core.Entities;

/// <summary>
/// Handles location setup and configuration workflows.
/// </summary>
public interface ILocationSetupService
{
    /// <summary>
    /// Creates a new location in the workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Location creation details</param>
    /// <param name="createdByUserId">User creating the location</param>
    /// <returns>The created location</returns>
    public Task<Location> CreateLocationAsync(int workspaceId, LocationCreationRequest request, int createdByUserId);

    /// <summary>
    /// Updates location details.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to update</param>
    /// <param name="request">Update details</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated location</returns>
    public Task<Location> UpdateLocationDetailsAsync(int workspaceId, int locationId, LocationUpdateRequest request, int updatedByUserId);

    /// <summary>
    /// Activates a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to activate</param>
    /// <param name="activatedByUserId">User performing activation</param>
    /// <returns>The activated location</returns>
    public Task<Location> ActivateLocationAsync(int workspaceId, int locationId, int activatedByUserId);

    /// <summary>
    /// Deactivates a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to deactivate</param>
    /// <param name="deactivatedByUserId">User performing deactivation</param>
    /// <returns>The deactivated location</returns>
    public Task<Location> DeactivateLocationAsync(int workspaceId, int locationId, int deactivatedByUserId);

    /// <summary>
    /// Assigns contacts to a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to assign contacts to</param>
    /// <param name="contactIds">Contact IDs to assign</param>
    /// <param name="assignedByUserId">User performing assignment</param>
    public Task AssignContactsToLocationAsync(int workspaceId, int locationId, List<int> contactIds, int assignedByUserId);

    /// <summary>
    /// Removes a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to remove</param>
    public Task RemoveLocationAsync(int workspaceId, int locationId);
}
