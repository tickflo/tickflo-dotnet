namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public interface IWorkspaceRepository
{
    public Task<Workspace?> FindBySlugAsync(string slug);
    public Task<Workspace?> FindByIdAsync(int id);
    public Task<Workspace> AddAsync(Workspace workspace);
    public Task UpdateAsync(Workspace workspace);
}


public class WorkspaceRepository(TickfloDbContext dbContext) : IWorkspaceRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public Task<Workspace?> FindBySlugAsync(string slug) => this.dbContext.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug);

    public Task<Workspace?> FindByIdAsync(int id) => this.dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == id);

    public async Task<Workspace> AddAsync(Workspace workspace)
    {
        this.dbContext.Workspaces.Add(workspace);
        await this.dbContext.SaveChangesAsync();
        System.Diagnostics.Debug.WriteLine($"DbContext Hash: ${this.dbContext.GetHashCode()}");
        return workspace;
    }

    public async Task UpdateAsync(Workspace workspace)
    {
        this.dbContext.Workspaces.Update(workspace);
        await this.dbContext.SaveChangesAsync();
    }
}
