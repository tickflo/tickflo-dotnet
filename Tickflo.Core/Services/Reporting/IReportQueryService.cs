using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Reporting;

public record ReportListItem(int Id, string Name, bool Ready, DateTime? LastRun);

public interface IReportQueryService
{
    Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default);
}


