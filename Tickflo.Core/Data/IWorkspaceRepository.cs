namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IWorkspaceRepository
{
    public Task<Workspace?> FindBySlugAsync(string slug);
    public Task<Workspace?> FindByIdAsync(int id);
    public Task AddAsync(Workspace workspace);
    public Task UpdateAsync(Workspace workspace);
}
