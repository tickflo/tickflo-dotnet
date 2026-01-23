namespace Tickflo.Web.Services;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;

// TODO: Idk about all this.. Maybe just make a cron job that calls an endpoint?
public class ScheduledReportsHostedService(IServiceProvider serviceProvider, ILogger<ScheduledReportsHostedService> logger) : BackgroundService
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<ScheduledReportsHostedService> logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run every minute
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = this.serviceProvider.CreateScope();
                var tickfloDbContext = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
                var reportRunService = scope.ServiceProvider.GetRequiredService<IReportRunService>();

                var now = DateTime.UtcNow;
                var due = await tickfloDbContext.Reports.AsNoTracking()
                    .Where(report => report.ScheduleEnabled && report.Ready)
                    .ToListAsync(stoppingToken);

                foreach (var report in due)
                {
                    if (!IsDue(report, now))
                    {
                        continue;
                    }

                    await reportRunService.RunReportAsync(report.WorkspaceId, report.Id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error running scheduled reports");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private static bool IsDue(Report report, DateTime utcNow)
    {
        if (!report.ScheduleEnabled)
        {
            return false;
        }

        var type = report.ScheduleType?.ToLowerInvariant() ?? "none";
        var last = report.LastRun;
        // Convert schedule time to today's UTC time; treat time as UTC for simplicity
        var scheduleTime = report.ScheduleTime;
        switch (type)
        {
            case "daily":
                if (scheduleTime == null)
                {
                    return false;
                }

                var todayRun = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day) + scheduleTime.Value;
                if (utcNow >= todayRun && (last == null || last.Value < todayRun))
                {
                    return true;
                }

                break;
            case "weekly":
                if (scheduleTime == null || report.ScheduleDayOfWeek is null)
                {
                    return false;
                }

                var startOfWeek = utcNow.Date.AddDays(-(int)utcNow.DayOfWeek);
                var weekRun = startOfWeek.AddDays(report.ScheduleDayOfWeek.Value) + scheduleTime.Value;
                if (utcNow >= weekRun && (last == null || last.Value < weekRun))
                {
                    return true;
                }

                break;
            case "monthly":
                if (scheduleTime == null || report.ScheduleDayOfMonth is null)
                {
                    return false;
                }

                var day = Math.Clamp(report.ScheduleDayOfMonth.Value, 1, DateTime.DaysInMonth(utcNow.Year, utcNow.Month));
                var monthRun = new DateTime(utcNow.Year, utcNow.Month, day) + scheduleTime.Value;
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
