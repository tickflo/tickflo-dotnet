using Microsoft.EntityFrameworkCore;

namespace Tickflo.Core.Data;

public class UserWorkspaceRoleRepository(TickfloDbContext db) : IUserWorkspaceRoleRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<bool> IsAdminAsync(int userId, int workspaceId)
    {
        return _db.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Join(_db.Roles, uwr => uwr.RoleId, r => r.Id, (uwr, r) => r.Admin)
            .AnyAsync(admin => admin);
    }

    public Task<List<string>> GetRoleNamesAsync(int userId, int workspaceId)
    {
        return _db.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Join(_db.Roles, uwr => uwr.RoleId, r => r.Id, (uwr, r) => r.Name)
            .ToListAsync();
    }

    public async Task AddAsync(int userId, int workspaceId, int roleId, int createdBy)
    {
        _db.UserWorkspaceRoles.Add(new Entities.UserWorkspaceRole
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<Entities.Role>> GetRolesAsync(int userId, int workspaceId)
    {
        return _db.UserWorkspaceRoles
            .Where(uwr => uwr.UserId == userId && uwr.WorkspaceId == workspaceId)
            .Join(_db.Roles, uwr => uwr.RoleId, r => r.Id, (uwr, r) => r)
            .ToListAsync();
    }

    public async Task RemoveAsync(int userId, int workspaceId, int roleId)
    {
        var mapping = await _db.UserWorkspaceRoles.FirstOrDefaultAsync(m => m.UserId == userId && m.WorkspaceId == workspaceId && m.RoleId == roleId);
        if (mapping != null)
        {
            _db.UserWorkspaceRoles.Remove(mapping);
            await _db.SaveChangesAsync();
        }
    }

    public Task<int> CountAssignmentsForRoleAsync(int workspaceId, int roleId)
    {
        return _db.UserWorkspaceRoles.CountAsync(m => m.WorkspaceId == workspaceId && m.RoleId == roleId);
    }
}
