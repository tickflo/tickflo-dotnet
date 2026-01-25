namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
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


public class LocationRepository(TickfloDbContext dbContext) : ILocationRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<IReadOnlyList<Location>> ListAsync(int workspaceId)
        => await this.dbContext.Locations.Where(l => l.WorkspaceId == workspaceId).OrderBy(l => l.Name).ToListAsync();

    public async Task<Location?> FindAsync(int workspaceId, int id)
        => await this.dbContext.Locations.FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == id);

    public async Task<Location> CreateAsync(Location location)
    {
        this.dbContext.Locations.Add(location);
        await this.dbContext.SaveChangesAsync();
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
        await this.dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id)
    {
        var loc = await this.FindAsync(workspaceId, id);
        if (loc == null)
        {
            return false;
        }

        this.dbContext.Locations.Remove(loc);
        await this.dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<int>> ListContactIdsAsync(int workspaceId, int locationId)
        => await this.dbContext.ContactLocations
            .Where(cl => cl.WorkspaceId == workspaceId && cl.LocationId == locationId)
            .Select(cl => cl.ContactId)
            .ToListAsync();

    public async Task SetContactsAsync(int workspaceId, int locationId, IReadOnlyList<int> contactIds)
    {
        var existing = await this.dbContext.ContactLocations
            .Where(cl => cl.WorkspaceId == workspaceId && cl.LocationId == locationId)
            .ToListAsync();
        var newSet = contactIds.Distinct().ToHashSet();
        foreach (var row in existing)
        {
            if (!newSet.Contains(row.ContactId))
            {
                this.dbContext.ContactLocations.Remove(row);
            }
        }
        var existingSet = existing.Select(e => e.ContactId).ToHashSet();
        foreach (var cid in newSet)
        {
            if (!existingSet.Contains(cid))
            {
                this.dbContext.ContactLocations.Add(new ContactLocation { WorkspaceId = workspaceId, LocationId = locationId, ContactId = cid });
            }
        }
        await this.dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> ListContactNamesAsync(int workspaceId, int locationId, int limit = 3)
    {
        var query = from cl in this.dbContext.ContactLocations
                    join c in this.dbContext.Contacts on new { cl.WorkspaceId, cl.ContactId } equals new { c.WorkspaceId, ContactId = c.Id }
                    where cl.WorkspaceId == workspaceId && cl.LocationId == locationId
                    orderby c.Name
                    select (c.Name ?? c.Email)
                    ;
        if (limit > 0)
        {
            query = query.Take(limit);
        }

        return await query.ToListAsync();
    }
}
