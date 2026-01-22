namespace Tickflo.Core.Services.Locations;

using Tickflo.Core.Data;
using static Tickflo.Core.Services.Locations.ILocationListingService;

public class LocationListingService(ILocationRepository locationRepository) : ILocationListingService
{
    private readonly ILocationRepository locationRepository = locationRepository;

    public async Task<IReadOnlyList<LocationItem>> GetListAsync(int workspaceId)
    {
        var list = await this.locationRepository.ListAsync(workspaceId);
        var items = new List<LocationItem>();

        foreach (var location in list)
        {
            var contactIds = await this.locationRepository.ListContactIdsAsync(workspaceId, location.Id);
            var contactCount = contactIds.Count;
            var previewNames = await this.locationRepository.ListContactNamesAsync(workspaceId, location.Id, 3);
            var preview = string.Join(", ", previewNames);

            items.Add(new LocationItem
            {
                Id = location.Id,
                Name = location.Name,
                Address = location.Address,
                Active = location.Active,
                ContactCount = contactCount,
                ContactPreview = preview
            });
        }

        return items.AsReadOnly();
    }
}



