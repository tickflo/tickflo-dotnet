namespace Tickflo.Core.Services.Locations;

using Tickflo.Core.Data;
using static Tickflo.Core.Services.Locations.ILocationListingService;

public class LocationListingService(ILocationRepository locationRepo) : ILocationListingService
{
    private readonly ILocationRepository _locationRepo = locationRepo;

    public async Task<IReadOnlyList<LocationItem>> GetListAsync(int workspaceId)
    {
        var list = await this._locationRepo.ListAsync(workspaceId);
        var items = new List<LocationItem>();

        foreach (var location in list)
        {
            var contactIds = await this._locationRepo.ListContactIdsAsync(workspaceId, location.Id);
            var contactCount = contactIds.Count;
            var previewNames = await this._locationRepo.ListContactNamesAsync(workspaceId, location.Id, 3);
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



