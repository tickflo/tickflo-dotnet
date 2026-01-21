namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Entities;

public record ReportExecutionResult(int RowCount, string FilePath, byte[] Bytes, string FileName, string ContentType);

public record ReportRunPage(
    int Page,
    int Take,
    int TotalRows,
    int TotalPages,
    int FromRow,
    int ToRow,
    bool HasContent,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public interface IReportingService
{
    public Task<ReportExecutionResult> ExecuteAsync(int workspaceId, Report report, CancellationToken ct = default);
    public Task<ReportRunPage> GetRunPageAsync(ReportRun run, int page, int take, CancellationToken ct = default);
    public IReadOnlyDictionary<string, string[]> GetAvailableSources();
}


