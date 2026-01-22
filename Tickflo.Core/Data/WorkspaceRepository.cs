namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class WorkspaceRepository(TickfloDbContext dbContext) : IWorkspaceRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<Workspace?> FindBySlugAsync(string slug) => this.dbContext.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug);

    public Task<Workspace?> FindByIdAsync(int id) => this.dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id);

    public async Task AddAsync(Workspace workspace)
    {
        this.dbContext.Workspaces.Add(workspace);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Workspace workspace)
    {
        this.dbContext.Workspaces.Update(workspace);
        await this.dbContext.SaveChangesAsync();
    }
}
