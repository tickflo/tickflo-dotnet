namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Entities;

/// <summary>
/// Service for executing and managing report runs.
/// Handles report execution, result generation, and pagination.
/// </summary>
public interface IReportExecutionService
{
    /// <summary>
    /// Executes a report and returns the result.
    /// </summary>
    public Task<ReportExecutionResult> ExecuteReportAsync(int userId, int workspaceId, int reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets a page of results from a report run.
    /// </summary>
    public Task<ReportRunPage> GetRunPageAsync(int runId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Gets available data sources for report design.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> GetAvailableDataSources();

    /// <summary>
    /// Schedules a report to run at a specific time.
    /// </summary>
    public Task<ReportRun> ScheduleReportAsync(int userId, int workspaceId, int reportId, DateTime scheduledFor, CancellationToken ct = default);

    /// <summary>
    /// Cancels a scheduled or running report.
    /// </summary>
    public Task CancelReportRunAsync(int userId, int workspaceId, int reportRunId, CancellationToken ct = default);

    /// <summary>
    /// Gets execution history for a report.
    /// </summary>
    public Task<IReadOnlyList<ReportRun>> GetReportHistoryAsync(int userId, int workspaceId, int reportId, int take = 20, CancellationToken ct = default);
}
