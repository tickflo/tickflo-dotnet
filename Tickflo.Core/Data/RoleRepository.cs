using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class RoleRepository(TickfloDbContext db) : IRoleRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<Role?> FindByNameAsync(int workspaceId, string name)
    {
        return _db.Roles.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Name == name);
    }

    public async Task<Role> AddAsync(int workspaceId, string name, bool admin, int createdBy)
    {
        var role = new Role
        {
            WorkspaceId = workspaceId,
            Name = name,
            Admin = admin,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    public Task<List<Role>> ListForWorkspaceAsync(int workspaceId)
    {
        return _db.Roles.Where(r => r.WorkspaceId == workspaceId).OrderBy(r => r.Name).ToListAsync();
    }

    public Task<Role?> FindByIdAsync(int id)
    {
        return _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task UpdateAsync(Role role)
    {
        _db.Roles.Update(role);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role != null)
        {
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
    }
}
