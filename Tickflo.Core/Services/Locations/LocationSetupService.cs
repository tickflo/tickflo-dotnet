namespace Tickflo.Core.Services.Locations;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of setting up and configuring locations.
/// </summary>
public class LocationSetupService(
    ILocationRepository locationRepository,
    IContactRepository contactRepository) : ILocationSetupService
{
    private readonly ILocationRepository locationRepository = locationRepository;
    private readonly IContactRepository contactRepository = contactRepository;

    /// <summary>
    /// Creates a new location with validation.
    /// </summary>
    public async Task<Location> CreateLocationAsync(
        int workspaceId,
        LocationCreationRequest request,
        int createdByUserId)
    {
        // Business rule: Location name must be unique within workspace
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Location name is required");
        }

        var name = request.Name.Trim();

        var existingLocations = await this.locationRepository.ListAsync(workspaceId);
        if (existingLocations.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Location '{name}' already exists in this workspace");
        }

        var location = new Location
        {
            WorkspaceId = workspaceId,
            Name = name,
            Address = string.IsNullOrWhiteSpace(request.Address) ? "" : request.Address.Trim(),
            Active = true // Business rule: New locations are active by default
        };

        await this.locationRepository.CreateAsync(location);

        return location;
    }

    /// <summary>
    /// Updates location details.
    /// </summary>
    public async Task<Location> UpdateLocationDetailsAsync(
        int workspaceId,
        int locationId,
        LocationUpdateRequest request,
        int updatedByUserId)
    {
        var location = await this.locationRepository.FindAsync(workspaceId, locationId) ?? throw new InvalidOperationException("Location not found");

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();

            // Check uniqueness if name is changing
            if (!string.Equals(location.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var existingLocations = await this.locationRepository.ListAsync(workspaceId);
                if (existingLocations.Any(l => l.Id != locationId &&
                    string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Location '{name}' already exists in this workspace");
                }
            }

            location.Name = name;
        }

        if (request.Address != null)
        {
            location.Address = string.IsNullOrWhiteSpace(request.Address) ? "" : request.Address.Trim();
        }

        await this.locationRepository.UpdateAsync(location);

        return location;
    }

    /// <summary>
    /// Activates a location.
    /// </summary>
    public async Task<Location> ActivateLocationAsync(int workspaceId, int locationId, int activatedByUserId)
    {
        var location = await this.locationRepository.FindAsync(workspaceId, locationId) ?? throw new InvalidOperationException("Location not found");

        if (location.Active)
        {
            return location; // Already active, no change needed
        }

        location.Active = true;

        await this.locationRepository.UpdateAsync(location);

        // Could add: Notify users, log activation, etc.

        return location;
    }

    /// <summary>
    /// Deactivates a location.
    /// </summary>
    public async Task<Location> DeactivateLocationAsync(int workspaceId, int locationId, int deactivatedByUserId)
    {
        var location = await this.locationRepository.FindAsync(workspaceId, locationId) ?? throw new InvalidOperationException("Location not found");

        if (!location.Active)
        {
            return location; // Already inactive
        }

        // Business rule: Could check for active tickets or inventory at this location

        location.Active = false;

        await this.locationRepository.UpdateAsync(location);

        // Could add: Reassign inventory, notify users, etc.

        return location;
    }

    /// <summary>
    /// Assigns contacts to a location.
    /// </summary>
    public async Task AssignContactsToLocationAsync(
        int workspaceId,
        int locationId,
        List<int> contactIds,
        int assignedByUserId)
    {
        var location = await this.locationRepository.FindAsync(workspaceId, locationId) ?? throw new InvalidOperationException("Location not found");

        // Business rule: Validate all contacts exist in the workspace
        if (contactIds.Count != 0)
        {
            var workspaceContacts = await this.contactRepository.ListAsync(workspaceId);
            var invalidContacts = contactIds.Except(workspaceContacts.Select(c => c.Id)).ToList();

            if (invalidContacts.Count != 0)
            {
                throw new InvalidOperationException($"Invalid contact IDs: {string.Join(", ", invalidContacts)}");
            }
        }

        // TODO: Implement contact assignment logic when schema supports it
        // This might involve a location_contacts join table

        await this.locationRepository.UpdateAsync(location);
    }

    /// <summary>
    /// Removes a location.
    /// </summary>
    public async Task RemoveLocationAsync(int workspaceId, int locationId)
    {
        var location = await this.locationRepository.FindAsync(workspaceId, locationId) ?? throw new InvalidOperationException("Location not found");

        // Business rule: Could prevent deletion if location has inventory or tickets

        await this.locationRepository.DeleteAsync(workspaceId, locationId);
    }
}

/// <summary>
/// Request to create a new location.
/// </summary>
public class LocationCreationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

/// <summary>
/// Request to update location details.
/// </summary>
public class LocationUpdateRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}
