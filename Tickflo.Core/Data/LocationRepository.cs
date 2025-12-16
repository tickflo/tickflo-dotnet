using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class LocationRepository(TickfloDbContext db) : ILocationRepository
{
    public async Task<IReadOnlyList<Location>> ListAsync(int workspaceId)
        => await db.Locations.Where(l => l.WorkspaceId == workspaceId).OrderBy(l => l.Name).ToListAsync();

    public async Task<Location?> FindAsync(int workspaceId, int id)
        => await db.Locations.FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == id);

    public async Task<Location> CreateAsync(Location location)
    {
        db.Locations.Add(location);
        await db.SaveChangesAsync();
        return location;
    }

    public async Task<Location?> UpdateAsync(Location location)
    {
        var existing = await FindAsync(location.WorkspaceId, location.Id);
        if (existing == null) return null;
        existing.Name = location.Name;
        existing.Address = location.Address;
        existing.Active = location.Active;
        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id)
    {
        var loc = await FindAsync(workspaceId, id);
        if (loc == null) return false;
        db.Locations.Remove(loc);
        await db.SaveChangesAsync();
        return true;
    }
}
