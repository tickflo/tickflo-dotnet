namespace Tickflo.Core.Services.Export;

using System.Text;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of data export service.
/// Handles formatting and streaming of large datasets.
/// </summary>
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

public class ExportService(TickfloDbContext dbContext) : IExportService
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<ExportResult> ExportTicketsAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId)
    {
        // Validate access
        var userAccess = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == exportingUserId && uw.WorkspaceId == workspaceId);

        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        var tickets = await this.dbContext.Tickets
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        return request.Format switch
        {
            ExportFormat.CSV => ExportToCSV(tickets, request),
            ExportFormat.JSON => ExportToJSON(tickets),
            ExportFormat.Excel => ExportToExcel(tickets, request),
            _ => throw new InvalidOperationException("Unsupported format.")
        };
    }

    public async Task<ExportResult> ExportContactsAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId)
    {
        var userAccess = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == exportingUserId && uw.WorkspaceId == workspaceId);

        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        var contacts = await this.dbContext.Contacts
            .Where(c => c.WorkspaceId == workspaceId)
            .ToListAsync();

        return request.Format switch
        {
            ExportFormat.CSV => ExportContactsToCSV(contacts),
            ExportFormat.JSON => ExportContactsToJSON(contacts),
            ExportFormat.Excel => throw new NotImplementedException(),
            _ => throw new InvalidOperationException("Unsupported format.")
        };
    }

    public async Task<ExportResult> ExportInventoryAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId)
    {
        var userAccess = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == exportingUserId && uw.WorkspaceId == workspaceId);

        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        var inventory = await this.dbContext.Inventory
            .Where(i => i.WorkspaceId == workspaceId)
            .ToListAsync();

        return request.Format switch
        {
            ExportFormat.CSV => ExportInventoryItemsToCSV(inventory),
            ExportFormat.JSON => ExportInventoryItemsToJSON(inventory),
            ExportFormat.Excel => throw new NotImplementedException(),
            _ => throw new InvalidOperationException("Unsupported format.")
        };
    }

    public async Task<ExportResult> ExportAuditAsync(
        int workspaceId,
        DateTime fromDate,
        DateTime toDate,
        int exportingUserId)
    {
        var userAccess = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == exportingUserId && uw.WorkspaceId == workspaceId);

        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        // In a real implementation, would have audit entries to export
        var auditData = new List<Dictionary<string, string>>();

        var content = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(auditData));

        return new ExportResult
        {
            Content = content,
            FileName = $"audit_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            RecordCount = auditData.Count
        };
    }

    public async Task<(bool IsValid, string ErrorMessage)> ValidateExportAsync(
        int workspaceId,
        ExportRequest request,
        int requestingUserId)
    {
        var userAccess = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == requestingUserId && uw.WorkspaceId == workspaceId);

        if (userAccess == null || !userAccess.Accepted)
        {
            return (false, "User does not have access to this workspace.");
        }

        if (string.IsNullOrWhiteSpace(request.EntityType))
        {
            return (false, "Entity type is required.");
        }

        if (!Enum.IsDefined(request.Format))
        {
            return (false, "Invalid export format.");
        }

        return (true, string.Empty);
    }

    private static ExportResult ExportToCSV(List<Ticket> tickets, ExportRequest request)
    {
        var sb = new StringBuilder();

        // Header
        var fields = request.Fields.Count > 0 ? request.Fields :
            ["Id", "Subject", "Status", "Priority", "Type", "CreatedAt"];
        sb.AppendLine(string.Join(",", fields.Select(f => $"\"{f}\"")));

        // Rows
        foreach (var ticket in tickets)
        {
            var row = new List<string>();
            foreach (var field in fields)
            {
                var value = GetFieldValue(ticket, field)?.ToString() ?? string.Empty;
                row.Add($"\"{value}\"");
            }
            sb.AppendLine(string.Join(",", row));
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return new ExportResult
        {
            Content = content,
            FileName = $"tickets_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = tickets.Count
        };
    }

    private static ExportResult ExportToJSON(List<Ticket> tickets)
    {
        var content = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(tickets));
        return new ExportResult
        {
            Content = content,
            FileName = $"tickets_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            RecordCount = tickets.Count
        };
    }

    private static ExportResult ExportToExcel(List<Ticket> tickets, ExportRequest request) =>
        // Excel export would require a library like EPPlus or OfficeOpenXml
        // For now, return a CSV as placeholder
        ExportToCSV(tickets, request);

    private static ExportResult ExportContactsToCSV(List<Contact> contacts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Email,Phone,Company,CreatedAt");

        foreach (var contact in contacts)
        {
            sb.AppendLine($"{contact.Id},\"{contact.Name}\",\"{contact.Email}\",\"{contact.Phone ?? ""}\",\"{contact.Company ?? ""}\",{contact.CreatedAt}");
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return new ExportResult
        {
            Content = content,
            FileName = $"contacts_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = contacts.Count
        };
    }

    private static ExportResult ExportContactsToJSON(List<Contact> contacts)
    {
        var content = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(contacts));
        return new ExportResult
        {
            Content = content,
            FileName = $"contacts_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            RecordCount = contacts.Count
        };
    }

    private static ExportResult ExportInventoryItemsToCSV(List<Inventory> inventory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,SKU,Name,Quantity,Cost,LocationId,CreatedAt");

        foreach (var item in inventory)
        {
            sb.AppendLine($"{item.Id},\"{item.Sku}\",\"{item.Name}\",{item.Quantity},{item.Cost},{item.LocationId},\"{item.CreatedAt}\"");
        }

        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return new ExportResult
        {
            Content = content,
            FileName = $"inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = inventory.Count
        };
    }

    private static ExportResult ExportInventoryItemsToJSON(List<Inventory> inventory)
    {
        var content = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(inventory));
        return new ExportResult
        {
            Content = content,
            FileName = $"inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            ContentType = "application/json",
            RecordCount = inventory.Count
        };
    }

    private static object? GetFieldValue(Ticket ticket, string fieldName) => fieldName switch
    {
        "Id" => ticket.Id,
        "Subject" => ticket.Subject,
        "Description" => ticket.Description,
        "StatusId" => ticket.StatusId,
        "PriorityId" => ticket.PriorityId,
        "TicketTypeId" => ticket.TicketTypeId,
        "CreatedAt" => ticket.CreatedAt,
        "UpdatedAt" => ticket.UpdatedAt,
        "ContactId" => ticket.ContactId,
        "LocationId" => ticket.LocationId,
        "AssignedUserId" => ticket.AssignedUserId,
        "AssignedTeamId" => ticket.AssignedTeamId,
        _ => null
    };
}
