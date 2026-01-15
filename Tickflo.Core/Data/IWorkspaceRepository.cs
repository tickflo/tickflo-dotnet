using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IWorkspaceRepository
{
    Task<Workspace?> FindBySlugAsync(string slug);
    Task<Workspace?> FindByIdAsync(int id);
    Task<Workspace?> FindByPortalTokenAsync(string token);
    Task AddAsync(Workspace workspace);
    Task UpdateAsync(Workspace workspace);
}
