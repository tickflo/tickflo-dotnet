using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public record ReportListItem(int Id, string Name, bool Ready, DateTime? LastRun);

public interface IReportQueryService
{
    Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default);
}
