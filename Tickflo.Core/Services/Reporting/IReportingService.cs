using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Reporting;

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
    Task<ReportExecutionResult> ExecuteAsync(int workspaceId, Report report, CancellationToken ct = default);
    Task<ReportRunPage> GetRunPageAsync(ReportRun run, int page, int take, CancellationToken ct = default);
    IReadOnlyDictionary<string, string[]> GetAvailableSources();
}


