using System.Text.Json;

namespace Tickflo.Core.Services;

public class ReportDefinitionValidator : IReportDefinitionValidator
{
    private static readonly IReadOnlyDictionary<string, string[]> AvailableSources = new Dictionary<string, string[]>
    {
        ["tickets"] = new []{ "Id","Subject","Description","Type","Priority","Status","AssignedUserId","AssignedTeamId","CreatedAt","UpdatedAt","ContactId","ChargeAmount","ChargeAmountAtLocation" },
        ["contacts"] = new []{ "Id","Name","Email","Phone","Company","Title","Priority","Status","AssignedUserId","LastInteraction","CreatedAt" },
        ["locations"] = new []{ "Id","Name","Address","Active","InventoryCount","TicketCount","OpenTicketCount","LastTicketAt" },
        ["inventory"] = new []{ "Id","Sku","Name","Description","Quantity","LocationId","MinStock","Cost","Price","Category","Status","Tags","LastRestockAt","CreatedAt","UpdatedAt","TicketCount","OpenTicketCount","LastTicketAt" },
    };

    public ReportDefinition Parse(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) 
                return new ReportDefinition("tickets", new []{"Id","Subject","Status","CreatedAt"}, null);
            
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var src = root.TryGetProperty("source", out var s) ? s.GetString() ?? "tickets" : "tickets";
            
            string[] fields = Array.Empty<string>();
            if (root.TryGetProperty("fields", out var f) && f.ValueKind == JsonValueKind.Array)
            {
                fields = f.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? "")
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }
            
            string? filtersJson = null;
            if (root.TryGetProperty("filters", out var fl))
            {
                filtersJson = fl.GetRawText();
            }
            
            return new ReportDefinition(src, fields, filtersJson);
        }
        catch 
        { 
            return new ReportDefinition("tickets", new []{"Id","Subject","Status","CreatedAt"}, null); 
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
