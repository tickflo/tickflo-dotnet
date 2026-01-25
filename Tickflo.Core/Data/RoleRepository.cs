namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class RoleRepository(TickfloDbContext dbContext) : IRoleRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<Role?> FindByNameAsync(int workspaceId, string name) => this.dbContext.Roles.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Name == name);

    public async Task<Role> AddAsync(Role role)
    {
        this.dbContext.Roles.Add(role);
        await this.dbContext.SaveChangesAsync();
        return role;
    }

    public Task<List<Role>> ListForWorkspaceAsync(int workspaceId) => this.dbContext.Roles.Where(r => r.WorkspaceId == workspaceId).OrderBy(r => r.Name).ToListAsync();

    public Task<Role?> FindByIdAsync(int id) => this.dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);

    public async Task UpdateAsync(Role role)
    {
        this.dbContext.Roles.Update(role);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await this.dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role != null)
        {
            this.dbContext.Roles.Remove(role);
            await this.dbContext.SaveChangesAsync();
        }
    }
}
