using Microsoft.Extensions.Logging;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class ReportRunService : IReportRunService
{
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _runRepo;
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportRunService> _logger;

    public ReportRunService(IReportRepository reportRepo, IReportRunRepository runRepo, IReportingService reportingService, ILogger<ReportRunService> logger)
    {
        _reportRepo = reportRepo;
        _runRepo = runRepo;
        _reportingService = reportingService;
        _logger = logger;
    }

    public async Task<(Report? Report, IReadOnlyList<ReportRun> Runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default)
    {
        var report = await _reportRepo.FindAsync(workspaceId, reportId);
        if (report == null) return (null, Array.Empty<ReportRun>());
        var runs = await _runRepo.ListForReportAsync(workspaceId, reportId, take);
        return (report, runs);
    }

    public async Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default)
    {
        var run = await _runRepo.FindAsync(workspaceId, runId);
        return run;
    }

    public async Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default)
    {
        var rep = await _reportRepo.FindAsync(workspaceId, reportId);
        if (rep == null) return null;

        var run = await _runRepo.CreateAsync(new ReportRun
        {
            WorkspaceId = workspaceId,
            ReportId = rep.Id,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        });

        await _runRepo.MarkRunningAsync(run.Id);

        try
        {
            var res = await _reportingService.ExecuteAsync(workspaceId, rep, ct);
            await _runRepo.CompleteAsync(run.Id, "Succeeded", res.RowCount, null, res.Bytes, res.ContentType, res.FileName);
            rep.LastRun = DateTime.UtcNow;
            await _reportRepo.UpdateAsync(rep);
            run.Status = "Succeeded";
            run.RowCount = res.RowCount;
            run.FileBytes = res.Bytes;
            run.ContentType = res.ContentType;
            run.FileName = res.FileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report run {ReportId} failed for workspace {WorkspaceId}", reportId, workspaceId);
            await _runRepo.CompleteAsync(run.Id, "Failed", 0, null);
            run.Status = "Failed";
        }

        return run;
    }
}
