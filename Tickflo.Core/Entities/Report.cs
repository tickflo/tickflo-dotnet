namespace Tickflo.Core.Entities;

public class Report
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Ready { get; set; } = false;
    public DateTime? LastRun { get; set; }
}
