namespace Tickflo.Core.Services.Reporting;

using System.Text.Json;
public record ReportDefinition(string Source, string[] Fields, string? FiltersJson);

public interface IReportDefinitionValidator
{
    public ReportDefinition Parse(string? json);
    public string BuildJson(string source, string fieldsCsv, string? filtersJson);
    public IReadOnlyDictionary<string, string[]> GetAvailableSources();
}


public class ReportDefinitionValidator : IReportDefinitionValidator
{
    private static readonly IReadOnlyDictionary<string, string[]> AvailableSources = new Dictionary<string, string[]>
    {
        ["tickets"] = ["Id", "Subject", "Description", "Type", "Priority", "Status", "AssignedUserId", "AssignedTeamId", "CreatedAt", "UpdatedAt", "ContactId", "ChargeAmount", "ChargeAmountAtLocation"],
        ["contacts"] = ["Id", "Name", "Email", "Phone", "Company", "Title", "Priority", "Status", "AssignedUserId", "LastInteraction", "CreatedAt"],
        ["locations"] = ["Id", "Name", "Address", "Active", "InventoryCount", "TicketCount", "OpenTicketCount", "LastTicketAt"],
        ["inventory"] = ["Id", "Sku", "Name", "Description", "Quantity", "LocationId", "MinStock", "Cost", "Price", "Category", "Status", "Tags", "LastRestockAt", "CreatedAt", "UpdatedAt", "TicketCount", "OpenTicketCount", "LastTicketAt"],
    };

    public ReportDefinition Parse(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ReportDefinition("tickets", ["Id", "Subject", "Status", "CreatedAt"], null);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var source = root.TryGetProperty("source", out var sourceElement) ? sourceElement.GetString() ?? "tickets" : "tickets";

            var fields = Array.Empty<string>();
            if (root.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Array)
            {
                fields = [.. fieldsElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? "")
                    .Where(x => !string.IsNullOrWhiteSpace(x))];
            }

            string? filtersJson = null;
            if (root.TryGetProperty("filters", out var filtersElement))
            {
                filtersJson = filtersElement.GetRawText();
            }

            return new ReportDefinition(source, fields, filtersJson);
        }
        catch
        {
            return new ReportDefinition("tickets", ["Id", "Subject", "Status", "CreatedAt"], null);
        }
    }

    public string BuildJson(string source, string fieldsCsv, string? filtersJson)
    {
        var fields = (fieldsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
        var filtersPart = string.IsNullOrWhiteSpace(filtersJson) ? "\"filters\":[]" : $"\"filters\":{filtersJson}";
        var json = $"{{\"source\":\"{(source ?? "tickets").ToLowerInvariant()}\",\"fields\":[{string.Join(',', fields.Select(f => $"\"{f}\""))}],{filtersPart}}}";
        return json;
    }

    public IReadOnlyDictionary<string, string[]> GetAvailableSources() => AvailableSources;
}

