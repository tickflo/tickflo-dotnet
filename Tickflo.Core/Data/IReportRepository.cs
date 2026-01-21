namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IReportRepository
{
    public Task<IReadOnlyList<Report>> ListAsync(int workspaceId);
    public Task<Report?> FindAsync(int workspaceId, int id);
    public Task<Report> CreateAsync(Report report);
    public Task<Report?> UpdateAsync(Report report);
    public Task<bool> DeleteAsync(int workspaceId, int id);
}
