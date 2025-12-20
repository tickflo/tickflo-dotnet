namespace Tickflo.Core.Entities;

public class Report
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Ready { get; set; } = false;
    public DateTime? LastRun { get; set; }

    // JSON definition describing data source, fields, filters, ordering
    public string? DefinitionJson { get; set; }

    // Scheduling (simple types to avoid cron dependency)
    public bool ScheduleEnabled { get; set; } = false;
    // one of: none, daily, weekly, monthly
    public string ScheduleType { get; set; } = "none";
    public TimeSpan? ScheduleTime { get; set; }
    public int? ScheduleDayOfWeek { get; set; } // 0=Sunday..6=Saturday
    public int? ScheduleDayOfMonth { get; set; } // 1..31
}
