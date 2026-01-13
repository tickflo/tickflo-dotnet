using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Locations;

/// <summary>
/// Handles the business workflow of setting up and configuring locations.
/// </summary>
public class LocationSetupService : ILocationSetupService
{
    private readonly ILocationRepository _locationRepo;
    private readonly IContactRepository _contactRepo;

    public LocationSetupService(
        ILocationRepository locationRepo,
        IContactRepository contactRepo)
    {
        _locationRepo = locationRepo;
        _contactRepo = contactRepo;
    }

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
            throw new InvalidOperationException("Location name is required");

        var name = request.Name.Trim();

        var existingLocations = await _locationRepo.ListAsync(workspaceId);
        if (existingLocations.Any(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Location '{name}' already exists in this workspace");

        var location = new Location
        {
            WorkspaceId = workspaceId,
            Name = name,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Active = true // Business rule: New locations are active by default
        };

        await _locationRepo.CreateAsync(location);

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
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            
            // Check uniqueness if name is changing
            if (!string.Equals(location.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var existingLocations = await _locationRepo.ListAsync(workspaceId);
                if (existingLocations.Any(l => l.Id != locationId && 
                    string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Location '{name}' already exists in this workspace");
            }

            location.Name = name;
        }

        if (request.Address != null)
            location.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

        await _locationRepo.UpdateAsync(location);

        return location;
    }

    /// <summary>
    /// Activates a location.
    /// </summary>
    public async Task<Location> ActivateLocationAsync(int workspaceId, int locationId, int activatedByUserId)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        if (location.Active)
            return location; // Already active, no change needed

        location.Active = true;

        await _locationRepo.UpdateAsync(location);

        // Could add: Notify users, log activation, etc.

        return location;
    }

    /// <summary>
    /// Deactivates a location.
    /// </summary>
    public async Task<Location> DeactivateLocationAsync(int workspaceId, int locationId, int deactivatedByUserId)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        if (!location.Active)
            return location; // Already inactive

        // Business rule: Could check for active tickets or inventory at this location
        
        location.Active = false;

        await _locationRepo.UpdateAsync(location);

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
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        // Business rule: Validate all contacts exist in the workspace
        if (contactIds.Any())
        {
            var workspaceContacts = await _contactRepo.ListAsync(workspaceId);
            var invalidContacts = contactIds.Except(workspaceContacts.Select(c => c.Id)).ToList();
            
            if (invalidContacts.Any())
                throw new InvalidOperationException($"Invalid contact IDs: {string.Join(", ", invalidContacts)}");
        }

        // TODO: Implement contact assignment logic when schema supports it
        // This might involve a location_contacts join table

        await _locationRepo.UpdateAsync(location);
    }

    /// <summary>
    /// Removes a location.
    /// </summary>
    public async Task RemoveLocationAsync(int workspaceId, int locationId)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        // Business rule: Could prevent deletion if location has inventory or tickets
        
        await _locationRepo.DeleteAsync(workspaceId, locationId);
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
