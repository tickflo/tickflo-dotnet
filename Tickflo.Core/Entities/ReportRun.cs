namespace Tickflo.Core.Entities;

public class ReportRun
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int ReportId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Running, Succeeded, Failed
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public int RowCount { get; set; }
    public string? FilePath { get; set; }
    public byte[]? FileBytes { get; set; }
    public string? ContentType { get; set; }
    public string? FileName { get; set; }
}
