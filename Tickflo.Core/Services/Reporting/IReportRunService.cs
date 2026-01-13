using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Reporting;

public interface IReportRunService
{
    Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default);
    Task<(Report? Report, IReadOnlyList<ReportRun> Runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default);
    Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default);
}


