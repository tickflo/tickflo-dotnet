namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class RoleRepository(TickfloDbContext db) : IRoleRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<Role?> FindByNameAsync(int workspaceId, string name) => this._db.Roles.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Name == name);

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
        this._db.Roles.Add(role);
        await this._db.SaveChangesAsync();
        return role;
    }

    public Task<List<Role>> ListForWorkspaceAsync(int workspaceId) => this._db.Roles.Where(r => r.WorkspaceId == workspaceId).OrderBy(r => r.Name).ToListAsync();

    public Task<Role?> FindByIdAsync(int id) => this._db.Roles.FirstOrDefaultAsync(r => r.Id == id);

    public async Task UpdateAsync(Role role)
    {
        this._db.Roles.Update(role);
        await this._db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await this._db.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role != null)
        {
            this._db.Roles.Remove(role);
            await this._db.SaveChangesAsync();
        }
    }
}
