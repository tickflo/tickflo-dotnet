using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IReportRepository
{
    Task<IReadOnlyList<Report>> ListAsync(int workspaceId);
    Task<Report?> FindAsync(int workspaceId, int id);
    Task<Report> CreateAsync(Report report);
    Task<Report?> UpdateAsync(Report report);
    Task<bool> DeleteAsync(int workspaceId, int id);
}
