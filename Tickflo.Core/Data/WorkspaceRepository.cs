using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class WorkspaceRepository(TickfloDbContext db) : IWorkspaceRepository
{
    private readonly TickfloDbContext _db = db;

    public Task<Workspace?> FindBySlugAsync(string slug)
    {
        return _db.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug);
    }

    public Task<Workspace?> FindByIdAsync(int id)
    {
        return _db.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task AddAsync(Workspace workspace)
    {
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync();
    }
}
