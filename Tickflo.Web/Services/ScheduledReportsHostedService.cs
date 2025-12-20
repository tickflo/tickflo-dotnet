using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Services;

public class ScheduledReportsHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<ScheduledReportsHostedService> _logger;

    public ScheduledReportsHostedService(IServiceProvider sp, ILogger<ScheduledReportsHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run every minute
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
                var runRepo = scope.ServiceProvider.GetRequiredService<IReportRunRepository>();
                var reportRepo = scope.ServiceProvider.GetRequiredService<IReportRepository>();
                var exec = scope.ServiceProvider.GetRequiredService<IReportingService>();

                var now = DateTime.UtcNow;
                var due = await db.Reports.AsNoTracking()
                    .Where(r => r.ScheduleEnabled && r.Ready)
                    .ToListAsync(stoppingToken);

                foreach (var r in due)
                {
                    if (!IsDue(r, now)) continue;
                    // Create run record
                    var run = await runRepo.CreateAsync(new ReportRun
                    {
                        WorkspaceId = r.WorkspaceId,
                        ReportId = r.Id,
                        Status = "Pending",
                        StartedAt = DateTime.UtcNow
                    });
                    await runRepo.MarkRunningAsync(run.Id);
                    try
                    {
                        var res = await exec.ExecuteAsync(r.WorkspaceId, r, stoppingToken);
                        await runRepo.CompleteAsync(run.Id, "Succeeded", res.RowCount, null, res.Bytes, res.ContentType, res.FileName);
                        r.LastRun = DateTime.UtcNow;
                        await reportRepo.UpdateAsync(r);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Scheduled report {ReportId} failed", r.Id);
                        await runRepo.CompleteAsync(run.Id, "Failed", 0, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled reports");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private bool IsDue(Report r, DateTime utcNow)
    {
        if (!r.ScheduleEnabled) return false;
        var type = r.ScheduleType?.ToLowerInvariant() ?? "none";
        var last = r.LastRun;
        // Convert schedule time to today's UTC time; treat time as UTC for simplicity
        var t = r.ScheduleTime;
        switch (type)
        {
            case "daily":
                if (t == null) return false;
                var todayRun = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day) + t.Value;
                if (utcNow >= todayRun && (last == null || last.Value < todayRun)) return true;
                break;
            case "weekly":
                if (t == null || r.ScheduleDayOfWeek is null) return false;
                var startOfWeek = utcNow.Date.AddDays(-(int)utcNow.DayOfWeek);
                var weekRun = startOfWeek.AddDays(r.ScheduleDayOfWeek.Value) + t.Value;
                if (utcNow >= weekRun && (last == null || last.Value < weekRun)) return true;
                break;
            case "monthly":
                if (t == null || r.ScheduleDayOfMonth is null) return false;
                var day = Math.Clamp(r.ScheduleDayOfMonth.Value, 1, DateTime.DaysInMonth(utcNow.Year, utcNow.Month));
                var monthRun = new DateTime(utcNow.Year, utcNow.Month, day) + t.Value;
                if (utcNow >= monthRun && (last == null || last.Value < monthRun)) return true;
                break;
        }
        return false;
    }
}
