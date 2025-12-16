using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> ListAsync(int workspaceId);
    Task<Location?> FindAsync(int workspaceId, int id);
    Task<Location> CreateAsync(Location location);
    Task<Location?> UpdateAsync(Location location);
    Task<bool> DeleteAsync(int workspaceId, int id);
}
