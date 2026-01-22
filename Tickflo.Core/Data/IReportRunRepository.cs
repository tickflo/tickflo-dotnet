namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IReportRunRepository
{
    public Task<ReportRun> CreateAsync(ReportRun run);
    public Task<bool> MarkRunningAsync(int id);
    public Task<bool> CompleteAsync(int id, string status, int rowCount, string? filePath, byte[]? fileBytes = null, string? contentType = null, string? fileName = null);
    public Task<IReadOnlyList<ReportRun>> ListForReportAsync(int workspaceId, int reportId, int take = 50);
    public Task<ReportRun?> FindAsync(int workspaceId, int id);
    public Task<int> DeleteForReportAsync(int workspaceId, int reportId);
    public Task<bool> UpdateContentAsync(int id, byte[] fileBytes, string contentType, string fileName);
    public Task<IReadOnlyList<ReportRun>> ListMissingContentAsync(int workspaceId, int? reportId = null, int take = 10000);
}
