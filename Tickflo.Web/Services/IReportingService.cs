using Tickflo.Core.Entities;

namespace Tickflo.Web.Services;

public record ReportExecutionResult(int RowCount, string FilePath, byte[] Bytes, string FileName, string ContentType);

public interface IReportingService
{
    Task<ReportExecutionResult> ExecuteAsync(int workspaceId, Report report, CancellationToken ct = default);
    IReadOnlyDictionary<string, string[]> GetAvailableSources();
}
