namespace Tickflo.Core.Services.Reporting;

using Microsoft.Extensions.Logging;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class ReportRunService(IReportRepository reportRepo, IReportRunRepository runRepo, IReportingService reportingService, ILogger<ReportRunService> logger) : IReportRunService
{
    private readonly IReportRepository _reportRepo = reportRepo;
    private readonly IReportRunRepository _runRepo = runRepo;
    private readonly IReportingService _reportingService = reportingService;
    private readonly ILogger<ReportRunService> _logger = logger;

    public async Task<(Report? Report, IReadOnlyList<ReportRun> Runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default)
    {
        var report = await this._reportRepo.FindAsync(workspaceId, reportId);
        if (report == null)
        {
            return (null, Array.Empty<ReportRun>());
        }

        var runs = await this._runRepo.ListForReportAsync(workspaceId, reportId, take);
        return (report, runs);
    }

    public async Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default)
    {
        var run = await this._runRepo.FindAsync(workspaceId, runId);
        return run;
    }

    public async Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default)
    {
        var rep = await this._reportRepo.FindAsync(workspaceId, reportId);
        if (rep == null)
        {
            return null;
        }

        var run = await this._runRepo.CreateAsync(new ReportRun
        {
            WorkspaceId = workspaceId,
            ReportId = rep.Id,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        });

        await this._runRepo.MarkRunningAsync(run.Id);

        try
        {
            var res = await this._reportingService.ExecuteAsync(workspaceId, rep, ct);
            await this._runRepo.CompleteAsync(run.Id, "Succeeded", res.RowCount, null, res.Bytes, res.ContentType, res.FileName);
            rep.LastRun = DateTime.UtcNow;
            await this._reportRepo.UpdateAsync(rep);
            run.Status = "Succeeded";
            run.RowCount = res.RowCount;
            run.FileBytes = res.Bytes;
            run.ContentType = res.ContentType;
            run.FileName = res.FileName;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Report run {ReportId} failed for workspace {WorkspaceId}", reportId, workspaceId);
            await this._runRepo.CompleteAsync(run.Id, "Failed", 0, null);
            run.Status = "Failed";
        }

        return run;
    }
}


