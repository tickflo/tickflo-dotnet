namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

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
        var existing = await this.FindAsync(location.WorkspaceId, location.Id);
        if (existing == null)
        {
            return null;
        }

        existing.Name = location.Name;
        existing.Address = location.Address;
        existing.Active = location.Active;
        existing.DefaultAssigneeUserId = location.DefaultAssigneeUserId;
        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id)
    {
        var loc = await this.FindAsync(workspaceId, id);
        if (loc == null)
        {
            return false;
        }

        db.Locations.Remove(loc);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<int>> ListContactIdsAsync(int workspaceId, int locationId)
        => await db.ContactLocations
            .Where(cl => cl.WorkspaceId == workspaceId && cl.LocationId == locationId)
            .Select(cl => cl.ContactId)
            .ToListAsync();

    public async Task SetContactsAsync(int workspaceId, int locationId, IReadOnlyList<int> contactIds)
    {
        var existing = await db.ContactLocations
            .Where(cl => cl.WorkspaceId == workspaceId && cl.LocationId == locationId)
            .ToListAsync();
        var newSet = contactIds.Distinct().ToHashSet();
        foreach (var row in existing)
        {
            if (!newSet.Contains(row.ContactId))
            {
                db.ContactLocations.Remove(row);
            }
        }
        var existingSet = existing.Select(e => e.ContactId).ToHashSet();
        foreach (var cid in newSet)
        {
            if (!existingSet.Contains(cid))
            {
                db.ContactLocations.Add(new ContactLocation { WorkspaceId = workspaceId, LocationId = locationId, ContactId = cid });
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> ListContactNamesAsync(int workspaceId, int locationId, int limit = 3)
    {
        var query = from cl in db.ContactLocations
                    join c in db.Contacts on new { cl.WorkspaceId, cl.ContactId } equals new { c.WorkspaceId, ContactId = c.Id }
                    where cl.WorkspaceId == workspaceId && cl.LocationId == locationId
                    orderby c.Name
                    select (c.Name ?? c.Email)!
                    ;
        if (limit > 0)
        {
            query = query.Take(limit);
        }

        return await query.ToListAsync();
    }
}
