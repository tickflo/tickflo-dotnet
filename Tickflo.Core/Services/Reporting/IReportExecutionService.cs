using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Reporting;

/// <summary>
/// Service for executing and managing report runs.
/// Handles report execution, result generation, and pagination.
/// </summary>
public interface IReportExecutionService
{
    /// <summary>
    /// Executes a report and returns the result.
    /// </summary>
    Task<ReportExecutionResult> ExecuteReportAsync(int userId, int workspaceId, int reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets a page of results from a report run.
    /// </summary>
    Task<ReportRunPage> GetRunPageAsync(int runId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Gets available data sources for report design.
    /// </summary>
    IReadOnlyDictionary<string, string[]> GetAvailableDataSources();

    /// <summary>
    /// Schedules a report to run at a specific time.
    /// </summary>
    Task<ReportRun> ScheduleReportAsync(int userId, int workspaceId, int reportId, DateTime scheduledFor, CancellationToken ct = default);

    /// <summary>
    /// Cancels a scheduled or running report.
    /// </summary>
    Task CancelReportRunAsync(int userId, int workspaceId, int reportRunId, CancellationToken ct = default);

    /// <summary>
    /// Gets execution history for a report.
    /// </summary>
    Task<IReadOnlyList<ReportRun>> GetReportHistoryAsync(int userId, int workspaceId, int reportId, int take = 20, CancellationToken ct = default);
}
