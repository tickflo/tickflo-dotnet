using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Reporting;

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
                var runSvc = scope.ServiceProvider.GetRequiredService<IReportRunService>();

                var now = DateTime.UtcNow;
                var due = await db.Reports.AsNoTracking()
                    .Where(r => r.ScheduleEnabled && r.Ready)
                    .ToListAsync(stoppingToken);

                foreach (var r in due)
                {
                    if (!IsDue(r, now)) continue;
                    await runSvc.RunReportAsync(r.WorkspaceId, r.Id, stoppingToken);
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

