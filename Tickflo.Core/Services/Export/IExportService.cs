namespace Tickflo.Core.Services.Export;

/// <summary>
/// Format for export output.
/// </summary>
public enum ExportFormat
{
    CSV,
    JSON,
    Excel
}

/// <summary>
/// Export request configuration.
/// </summary>
public class ExportRequest
{
    public ExportFormat Format { get; set; } = ExportFormat.CSV;
    public string EntityType { get; set; } = string.Empty; // "Tickets", "Contacts", "Inventory", etc.
    public List<string> Fields { get; set; } = []; // Specific fields to include
    public Dictionary<string, string>? Filters { get; set; } // Filter criteria
    public DateTime? ExportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Behavior-focused service for exporting data in various formats.
/// Handles large datasets efficiently with streaming and formatting.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Export tickets with specified filters and format.
    /// Returns file content and metadata for download.
    /// </summary>
    public Task<ExportResult> ExportTicketsAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId);

    /// <summary>
    /// Export contacts with optional filter.
    /// </summary>
    public Task<ExportResult> ExportContactsAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId);

    /// <summary>
    /// Export inventory items.
    /// </summary>
    public Task<ExportResult> ExportInventoryAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId);

    /// <summary>
    /// Export ticket history/audit trail.
    /// </summary>
    public Task<ExportResult> ExportAuditAsync(
        int workspaceId,
        DateTime fromDate,
        DateTime toDate,
        int exportingUserId);

    /// <summary>
    /// Validate export request before processing.
    /// </summary>
    public Task<(bool IsValid, string ErrorMessage)> ValidateExportAsync(
        int workspaceId,
        ExportRequest request,
        int requestingUserId);
}

/// <summary>
/// Result of an export operation.
/// </summary>
public class ExportResult
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public int RecordCount { get; set; }
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
