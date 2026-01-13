namespace Tickflo.Core.Services.Reporting;

public record ReportDefinition(string Source, string[] Fields, string? FiltersJson);

public interface IReportDefinitionValidator
{
    ReportDefinition Parse(string? json);
    string BuildJson(string source, string fieldsCsv, string? filtersJson);
    IReadOnlyDictionary<string, string[]> GetAvailableSources();
}


