using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for managing locations.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Creates a new location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Location creation details</param>
    /// <returns>Created location</returns>
    Task<Location> CreateLocationAsync(int workspaceId, CreateLocationRequest request);

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to update</param>
    /// <param name="request">Update details</param>
    /// <returns>Updated location</returns>
    Task<Location> UpdateLocationAsync(int workspaceId, int locationId, UpdateLocationRequest request);

    /// <summary>
    /// Deletes a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to delete</param>
    Task DeleteLocationAsync(int workspaceId, int locationId);

    /// <summary>
    /// Updates contact assignments for a location.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="locationId">Location to update</param>
    /// <param name="contactIds">Contact IDs to assign</param>
    Task UpdateContactAssignmentsAsync(int workspaceId, int locationId, List<int> contactIds);
}

public class CreateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public int? DefaultAssigneeUserId { get; set; }
}

public class UpdateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public int? DefaultAssigneeUserId { get; set; }
}
