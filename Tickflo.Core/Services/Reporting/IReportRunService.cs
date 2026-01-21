namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Entities;

public interface IReportRunService
{
    public Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default);
    public Task<(Report? Report, IReadOnlyList<ReportRun> Runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default);
    public Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default);
}


