namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserWorkspaceRoleRepository(TickfloDbContext dbContext) : IUserWorkspaceRoleRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<bool> IsAdminAsync(int userId, int workspaceId) => this.dbContext.UserWorkspaceRoles
            .Where(userWorkspaceRoleRepository => userWorkspaceRoleRepository.UserId == userId && userWorkspaceRoleRepository.WorkspaceId == workspaceId)
            .Join(this.dbContext.Roles, userWorkspaceRoleRepository => userWorkspaceRoleRepository.RoleId, r => r.Id, (userWorkspaceRoleRepository, r) => r.Admin)
            .AnyAsync(admin => admin);

    public Task<List<string>> GetRoleNamesAsync(int userId, int workspaceId) => this.dbContext.UserWorkspaceRoles
            .Where(userWorkspaceRoleRepository => userWorkspaceRoleRepository.UserId == userId && userWorkspaceRoleRepository.WorkspaceId == workspaceId)
            .Join(this.dbContext.Roles, userWorkspaceRoleRepository => userWorkspaceRoleRepository.RoleId, r => r.Id, (userWorkspaceRoleRepository, r) => r.Name)
            .ToListAsync();

    public async Task<UserWorkspaceRole> AddAsync(UserWorkspaceRole userWorkspaceRole)
    {
        this.dbContext.UserWorkspaceRoles.Add(userWorkspaceRole);
        await this.dbContext.SaveChangesAsync();
        return userWorkspaceRole;
    }

    public Task<List<Role>> GetRolesAsync(int userId, int workspaceId) => this.dbContext.UserWorkspaceRoles
            .Where(userWorkspaceRoleRepository => userWorkspaceRoleRepository.UserId == userId && userWorkspaceRoleRepository.WorkspaceId == workspaceId)
            .Join(this.dbContext.Roles, userWorkspaceRoleRepository => userWorkspaceRoleRepository.RoleId, r => r.Id, (userWorkspaceRoleRepository, r) => r)
            .ToListAsync();

    public async Task RemoveAsync(int userId, int workspaceId, int roleId)
    {
        var mapping = await this.dbContext.UserWorkspaceRoles.FirstOrDefaultAsync(m => m.UserId == userId && m.WorkspaceId == workspaceId && m.RoleId == roleId);
        if (mapping != null)
        {
            this.dbContext.UserWorkspaceRoles.Remove(mapping);
            await this.dbContext.SaveChangesAsync();
        }
    }

    public Task<int> CountAssignmentsForRoleAsync(int workspaceId, int roleId) => this.dbContext.UserWorkspaceRoles.CountAsync(m => m.WorkspaceId == workspaceId && m.RoleId == roleId);
}
