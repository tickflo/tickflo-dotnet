namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;

public class UserWorkspaceRoleRepository(TickfloDbContext db) : IUserWorkspaceRoleRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<bool> IsAdminAsync(int userId, int workspaceId) => this._db.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Join(this._db.Roles, uwr => uwr.RoleId, r => r.Id, (uwr, r) => r.Admin)
            .AnyAsync(admin => admin);

    public Task<List<string>> GetRoleNamesAsync(int userId, int workspaceId) => this._db.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Join(this._db.Roles, uwr => uwr.RoleId, r => r.Id, (uwr, r) => r.Name)
            .ToListAsync();

    public async Task AddAsync(int userId, int workspaceId, int roleId, int createdBy)
    {
        this._db.UserWorkspaceRoles.Add(new Entities.UserWorkspaceRole
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        });
        await this._db.SaveChangesAsync();
    }

    public Task<List<Entities.Role>> GetRolesAsync(int userId, int workspaceId) => this._db.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Join(this._db.Roles, uwr => uwr.RoleId, r => r.Id, (uwr, r) => r)
            .ToListAsync();

    public async Task RemoveAsync(int userId, int workspaceId, int roleId)
    {
        var mapping = await this._db.UserWorkspaceRoles.FirstOrDefaultAsync(m => m.UserId == userId && m.WorkspaceId == workspaceId && m.RoleId == roleId);
        if (mapping != null)
        {
            this._db.UserWorkspaceRoles.Remove(mapping);
            await this._db.SaveChangesAsync();
        }
    }

    public Task<int> CountAssignmentsForRoleAsync(int workspaceId, int roleId) => this._db.UserWorkspaceRoles.CountAsync(m => m.WorkspaceId == workspaceId && m.RoleId == roleId);
}
