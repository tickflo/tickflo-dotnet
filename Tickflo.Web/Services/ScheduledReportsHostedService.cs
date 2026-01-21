namespace Tickflo.Web.Services;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;

public class ScheduledReportsHostedService(IServiceProvider sp, ILogger<ScheduledReportsHostedService> logger) : BackgroundService
{
    private readonly IServiceProvider _sp = sp;
    private readonly ILogger<ScheduledReportsHostedService> logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run every minute
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = this._sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
                var runSvc = scope.ServiceProvider.GetRequiredService<IReportRunService>();

                var now = DateTime.UtcNow;
                var due = await db.Reports.AsNoTracking()
                    .Where(r => r.ScheduleEnabled && r.Ready)
                    .ToListAsync(stoppingToken);

                foreach (var r in due)
                {
                    if (!IsDue(r, now))
                    {
                        continue;
                    }

                    await runSvc.RunReportAsync(r.WorkspaceId, r.Id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error running scheduled reports");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private static bool IsDue(Report r, DateTime utcNow)
    {
        if (!r.ScheduleEnabled)
        {
            return false;
        }

        var type = r.ScheduleType?.ToLowerInvariant() ?? "none";
        var last = r.LastRun;
        // Convert schedule time to today's UTC time; treat time as UTC for simplicity
        var t = r.ScheduleTime;
        switch (type)
        {
            case "daily":
                if (t == null)
                {
                    return false;
                }

                var todayRun = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day) + t.Value;
                if (utcNow >= todayRun && (last == null || last.Value < todayRun))
                {
                    return true;
                }

                break;
            case "weekly":
                if (t == null || r.ScheduleDayOfWeek is null)
                {
                    return false;
                }

                var startOfWeek = utcNow.Date.AddDays(-(int)utcNow.DayOfWeek);
                var weekRun = startOfWeek.AddDays(r.ScheduleDayOfWeek.Value) + t.Value;
                if (utcNow >= weekRun && (last == null || last.Value < weekRun))
                {
                    return true;
                }

                break;
            case "monthly":
                if (t == null || r.ScheduleDayOfMonth is null)
                {
                    return false;
                }

                var day = Math.Clamp(r.ScheduleDayOfMonth.Value, 1, DateTime.DaysInMonth(utcNow.Year, utcNow.Month));
                var monthRun = new DateTime(utcNow.Year, utcNow.Month, day) + t.Value;
                if (utcNow >= monthRun && (last == null || last.Value < monthRun))
                {
                    return true;
                }

                break;
            default:
                break;
        }
        return false;
    }
}

