using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IReportRunRepository
{
    Task<ReportRun> CreateAsync(ReportRun run);
    Task<bool> MarkRunningAsync(int id);
    Task<bool> CompleteAsync(int id, string status, int rowCount, string? filePath, byte[]? fileBytes = null, string? contentType = null, string? fileName = null);
    Task<IReadOnlyList<ReportRun>> ListForReportAsync(int workspaceId, int reportId, int take = 50);
    Task<ReportRun?> FindAsync(int workspaceId, int id);
    Task<int> DeleteForReportAsync(int workspaceId, int reportId);
    Task<bool> UpdateContentAsync(int id, byte[] fileBytes, string contentType, string fileName);
    Task<IReadOnlyList<ReportRun>> ListMissingContentAsync(int workspaceId, int? reportId = null, int take = 10000);
}
