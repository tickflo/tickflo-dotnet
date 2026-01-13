using System.Text;
using Tickflo.Core.Entities;
using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Export;

/// <summary>
/// Implementation of data export service.
/// Handles formatting and streaming of large datasets.
/// </summary>
public class ExportService : IExportService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;

    public ExportService(
        ITicketRepository ticketRepo,
        IContactRepository contactRepo,
        IInventoryRepository inventoryRepo,
        ITicketHistoryRepository historyRepo,
        IUserWorkspaceRepository userWorkspaceRepo)
    {
        _ticketRepo = ticketRepo;
        _contactRepo = contactRepo;
        _inventoryRepo = inventoryRepo;
        _historyRepo = historyRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
    }

    public async Task<ExportResult> ExportTicketsAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId)
    {
        // Validate access
        var userAccess = await _userWorkspaceRepo.FindAsync(exportingUserId, workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        var tickets = (await _ticketRepo.ListAsync(workspaceId)).ToList();

        return request.Format switch
        {
            ExportFormat.CSV => ExportToCSV(tickets, request),
            ExportFormat.JSON => ExportToJSON(tickets, request),
            ExportFormat.Excel => ExportToExcel(tickets, request),
            _ => throw new InvalidOperationException("Unsupported format.")
        };
    }

    public async Task<ExportResult> ExportContactsAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId)
    {
        var userAccess = await _userWorkspaceRepo.FindAsync(exportingUserId, workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        var contacts = (await _contactRepo.ListAsync(workspaceId)).ToList();

        return request.Format switch
        {
            ExportFormat.CSV => ExportContactsToCSV(contacts, request),
            ExportFormat.JSON => ExportContactsToJSON(contacts, request),
            _ => throw new InvalidOperationException("Unsupported format.")
        };
    }

    public async Task<ExportResult> ExportInventoryAsync(
        int workspaceId,
        ExportRequest request,
        int exportingUserId)
    {
        var userAccess = await _userWorkspaceRepo.FindAsync(exportingUserId, workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            throw new InvalidOperationException("User does not have access to this workspace.");
        }

        var inventory = (await _inventoryRepo.ListAsync(workspaceId)).ToList();

        return request.Format switch
        {
            ExportFormat.CSV => ExportInventoryItemsToCSV(inventory, request),
            ExportFormat.JSON => ExportInventoryItemsToJSON(inventory, request),
            _ => throw new InvalidOperationException("Unsupported format.")
        };
    }

    public async Task<ExportResult> ExportAuditAsync(
        int workspaceId,
        DateTime fromDate,
        DateTime toDate,
        int exportingUserId)
    {
        var userAccess = await _userWorkspaceRepo.FindAsync(exportingUserId, workspaceId);
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
        var userAccess = await _userWorkspaceRepo.FindAsync(requestingUserId, workspaceId);
        if (userAccess == null || !userAccess.Accepted)
        {
            return (false, "User does not have access to this workspace.");
        }

        if (string.IsNullOrWhiteSpace(request.EntityType))
        {
            return (false, "Entity type is required.");
        }

        if (!Enum.IsDefined(typeof(ExportFormat), request.Format))
        {
            return (false, "Invalid export format.");
        }

        return (true, string.Empty);
    }

    private ExportResult ExportToCSV(List<Ticket> tickets, ExportRequest request)
    {
        var sb = new StringBuilder();
        
        // Header
        var fields = request.Fields.Count > 0 ? request.Fields : 
            new List<string> { "Id", "Subject", "Status", "Priority", "Type", "CreatedAt" };
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

    private ExportResult ExportToJSON(List<Ticket> tickets, ExportRequest request)
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

    private ExportResult ExportToExcel(List<Ticket> tickets, ExportRequest request)
    {
        // Excel export would require a library like EPPlus or OfficeOpenXml
        // For now, return a CSV as placeholder
        return ExportToCSV(tickets, request);
    }

    private ExportResult ExportContactsToCSV(List<Contact> contacts, ExportRequest request)
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

    private ExportResult ExportContactsToJSON(List<Contact> contacts, ExportRequest request)
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

    private ExportResult ExportInventoryItemsToCSV(List<Entities.Inventory> inventory, ExportRequest request)
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

    private ExportResult ExportInventoryItemsToJSON(List<Entities.Inventory> inventory, ExportRequest request)
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

    private object? GetFieldValue(Ticket ticket, string fieldName)
    {
        return fieldName switch
        {
            "Id" => ticket.Id,
            "Subject" => ticket.Subject,
            "Description" => ticket.Description,
            "Status" => ticket.Status,
            "Priority" => ticket.Priority,
            "Type" => ticket.Type,
            "CreatedAt" => ticket.CreatedAt,
            "UpdatedAt" => ticket.UpdatedAt,
            "ContactId" => ticket.ContactId,
            "LocationId" => ticket.LocationId,
            "AssignedUserId" => ticket.AssignedUserId,
            "AssignedTeamId" => ticket.AssignedTeamId,
            _ => null
        };
    }
}
