using Tickflo.Core.Data;
using Tickflo.Core.Services;
using static Tickflo.Core.Services.ILocationListingService;

namespace Tickflo.Core.Services;

public class LocationListingService : ILocationListingService
{
    private readonly ILocationRepository _locationRepo;

    public LocationListingService(ILocationRepository locationRepo)
    {
        _locationRepo = locationRepo;
    }

    public async Task<IReadOnlyList<LocationItem>> GetListAsync(int workspaceId)
    {
        var list = await _locationRepo.ListAsync(workspaceId);
        var items = new List<LocationItem>();
        
        foreach (var location in list)
        {
            var contactIds = await _locationRepo.ListContactIdsAsync(workspaceId, location.Id);
            var contactCount = contactIds.Count;
            var previewNames = await _locationRepo.ListContactNamesAsync(workspaceId, location.Id, 3);
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
