namespace Tickflo.Core.Services.Reporting;

using Microsoft.Extensions.Logging;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IReportRunService
{
    public Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default);
    public Task<(Report? report, IReadOnlyList<ReportRun> runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default);
    public Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default);
}


public class ReportRunService(IReportRepository reporyRepository, IReportRunRepository reportRunRepository, IReportingService reportingService, ILogger<ReportRunService> logger) : IReportRunService
{
    private readonly IReportRepository reporyRepository = reporyRepository;
    private readonly IReportRunRepository reportRunRepository = reportRunRepository;
    private readonly IReportingService reportingService = reportingService;
    private readonly ILogger<ReportRunService> logger = logger;

    public async Task<(Report? report, IReadOnlyList<ReportRun> runs)> GetReportRunsAsync(int workspaceId, int reportId, int take = 100, CancellationToken ct = default)
    {
        var report = await this.reporyRepository.FindAsync(workspaceId, reportId);
        if (report == null)
        {
            return (null, Array.Empty<ReportRun>());
        }

        var runs = await this.reportRunRepository.ListForReportAsync(workspaceId, reportId, take);
        return (report, runs);
    }

    public async Task<ReportRun?> GetRunAsync(int workspaceId, int runId, CancellationToken ct = default)
    {
        var run = await this.reportRunRepository.FindAsync(workspaceId, runId);
        return run;
    }

    public async Task<ReportRun?> RunReportAsync(int workspaceId, int reportId, CancellationToken ct = default)
    {
        var rep = await this.reporyRepository.FindAsync(workspaceId, reportId);
        if (rep == null)
        {
            return null;
        }

        var run = await this.reportRunRepository.CreateAsync(new ReportRun
        {
            WorkspaceId = workspaceId,
            ReportId = rep.Id,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        });

        await this.reportRunRepository.MarkRunningAsync(run.Id);

        try
        {
            var res = await this.reportingService.ExecuteAsync(workspaceId, rep, ct);
            await this.reportRunRepository.CompleteAsync(run.Id, "Succeeded", res.RowCount, null, res.Bytes, res.ContentType, res.FileName);
            rep.LastRun = DateTime.UtcNow;
            await this.reporyRepository.UpdateAsync(rep);
            run.Status = "Succeeded";
            run.RowCount = res.RowCount;
            run.FileBytes = res.Bytes;
            run.ContentType = res.ContentType;
            run.FileName = res.FileName;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Report run {ReportId} failed for workspace {WorkspaceId}", reportId, workspaceId);
            await this.reportRunRepository.CompleteAsync(run.Id, "Failed", 0, null);
            run.Status = "Failed";
        }

        return run;
    }
}


