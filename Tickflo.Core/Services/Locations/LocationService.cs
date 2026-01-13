using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Locations;

/// <summary>
/// Service for managing locations.
/// </summary>
public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;

    public LocationService(
        ILocationRepository locationRepo,
        IUserWorkspaceRepository userWorkspaceRepo)
    {
        _locationRepo = locationRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
    }

    public async Task<Location> CreateLocationAsync(int workspaceId, CreateLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Location name is required");

        var name = request.Name.Trim();

        // Validate default assignee if provided
        if (request.DefaultAssigneeUserId.HasValue)
        {
            var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
            if (!memberships.Any(m => m.UserId == request.DefaultAssigneeUserId.Value && m.Accepted))
                throw new InvalidOperationException("Default assignee is not a workspace member");
        }

        var location = new Location
        {
            WorkspaceId = workspaceId,
            Name = name,
            Address = string.IsNullOrWhiteSpace(request.Address) ? string.Empty : request.Address.Trim(),
            DefaultAssigneeUserId = request.DefaultAssigneeUserId,
            Active = true
        };

        await _locationRepo.CreateAsync(location);

        return location;
    }

    public async Task<Location> UpdateLocationAsync(int workspaceId, int locationId, UpdateLocationRequest request)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Location name is required");

        // Validate default assignee if provided
        if (request.DefaultAssigneeUserId.HasValue)
        {
            var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
            if (!memberships.Any(m => m.UserId == request.DefaultAssigneeUserId.Value && m.Accepted))
                throw new InvalidOperationException("Default assignee is not a workspace member");
        }

        location.Name = request.Name.Trim();
        location.Address = string.IsNullOrWhiteSpace(request.Address) ? string.Empty : request.Address.Trim();
        location.DefaultAssigneeUserId = request.DefaultAssigneeUserId;

        await _locationRepo.UpdateAsync(location);

        return location;
    }

    public async Task DeleteLocationAsync(int workspaceId, int locationId)
    {
        await _locationRepo.DeleteAsync(workspaceId, locationId);
    }

    public async Task UpdateContactAssignmentsAsync(int workspaceId, int locationId, List<int> contactIds)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location == null)
            throw new InvalidOperationException("Location not found");

        // Note: Contact assignments for locations are not yet implemented in the repository layer
        // This would require a LocationContact junction table and repository
        // TODO: Implement location-contact association when repository support is added
        await Task.CompletedTask;
    }
}



