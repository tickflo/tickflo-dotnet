namespace Tickflo.Core.Services.Reporting;

public record ReportListItem(int Id, string Name, bool Ready, DateTime? LastRun);

public interface IReportQueryService
{
    public Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default);
}


