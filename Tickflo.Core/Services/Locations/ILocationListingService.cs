namespace Tickflo.Core.Services.Locations;

public interface ILocationListingService
{
    public record LocationItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int ContactCount { get; set; }
        public string ContactPreview { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gets enriched location items for a workspace with contact preview info.
    /// </summary>
    public Task<IReadOnlyList<LocationItem>> GetListAsync(int workspaceId);
}



