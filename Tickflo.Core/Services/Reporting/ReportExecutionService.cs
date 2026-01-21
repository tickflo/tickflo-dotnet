namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class ReportExecutionService(
    IReportRepository reportRepo,
    IReportRunRepository reportRunRepo,
    IUserWorkspaceRepository workspaceRepo,
    IReportingService reportingService) : IReportExecutionService
{
    private readonly IReportRepository _reportRepo = reportRepo;
    private readonly IReportRunRepository _reportRunRepo = reportRunRepo;
    private readonly IUserWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IReportingService _reportingService = reportingService;

    public async Task<ReportExecutionResult> ExecuteReportAsync(int userId, int workspaceId, int reportId, CancellationToken ct = default)
    {
        var workspace = await this._workspaceRepo.FindAsync(userId, workspaceId) ?? throw new UnauthorizedAccessException();

        var report = await this._reportRepo.FindAsync(workspaceId, reportId) ?? throw new KeyNotFoundException();

        return await this._reportingService.ExecuteAsync(workspaceId, report, ct);
    }

    public async Task<ReportRunPage> GetRunPageAsync(int runId, int page, int pageSize, CancellationToken ct = default) =>
        // Note: ReportRun doesn't expose workspace_id directly, would need to refactor this
        // For now, assume workspaceId can be passed or derived from context
        throw new NotImplementedException("Requires workspace context to be added to ReportRun");

    public IReadOnlyDictionary<string, string[]> GetAvailableDataSources() => this._reportingService.GetAvailableSources();

    public async Task<ReportRun> ScheduleReportAsync(int userId, int workspaceId, int reportId, DateTime scheduledFor, CancellationToken ct = default)
    {
        var workspace = await this._workspaceRepo.FindAsync(userId, workspaceId) ?? throw new UnauthorizedAccessException();

        var report = await this._reportRepo.FindAsync(workspaceId, reportId) ?? throw new KeyNotFoundException();

        if (scheduledFor < DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled time must be in the future");
        }

        // This is a simplified version - actual scheduling would need a background job
        var run = new ReportRun { ReportId = reportId };
        return await this._reportRunRepo.CreateAsync(run);
    }

    public async Task CancelReportRunAsync(int userId, int workspaceId, int reportRunId, CancellationToken ct = default)
    {
        var workspace = await this._workspaceRepo.FindAsync(userId, workspaceId) ?? throw new UnauthorizedAccessException();

        var run = await this._reportRunRepo.FindAsync(workspaceId, reportRunId) ?? throw new KeyNotFoundException();
    }

    public async Task<IReadOnlyList<ReportRun>> GetReportHistoryAsync(int userId, int workspaceId, int reportId, int take = 20, CancellationToken ct = default)
    {
        var workspace = await this._workspaceRepo.FindAsync(userId, workspaceId) ?? throw new UnauthorizedAccessException();

        var report = await this._reportRepo.FindAsync(workspaceId, reportId) ?? throw new KeyNotFoundException();

        return await this._reportRunRepo.ListForReportAsync(workspaceId, reportId, take);
    }
}
