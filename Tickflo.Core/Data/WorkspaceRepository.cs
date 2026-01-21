namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class WorkspaceRepository(TickfloDbContext db) : IWorkspaceRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<Workspace?> FindBySlugAsync(string slug) => this._db.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug);

    public Task<Workspace?> FindByIdAsync(int id) => this._db.Workspaces.FirstOrDefaultAsync(w => w.Id == id);

    public async Task AddAsync(Workspace workspace)
    {
        this._db.Workspaces.Add(workspace);
        await this._db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Workspace workspace)
    {
        this._db.Workspaces.Update(workspace);
        await this._db.SaveChangesAsync();
    }
}
