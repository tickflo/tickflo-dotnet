namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface ILocationRepository
{
    public Task<IReadOnlyList<Location>> ListAsync(int workspaceId);
    public Task<Location?> FindAsync(int workspaceId, int id);
    public Task<Location> CreateAsync(Location location);
    public Task<Location?> UpdateAsync(Location location);
    public Task<bool> DeleteAsync(int workspaceId, int id);
    public Task<IReadOnlyList<int>> ListContactIdsAsync(int workspaceId, int locationId);
    public Task SetContactsAsync(int workspaceId, int locationId, IReadOnlyList<int> contactIds);
    public Task<IReadOnlyList<string>> ListContactNamesAsync(int workspaceId, int locationId, int limit = 3);
}
