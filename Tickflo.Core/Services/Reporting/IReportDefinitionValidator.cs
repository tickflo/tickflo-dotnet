namespace Tickflo.Core.Services.Reporting;

public record ReportDefinition(string Source, string[] Fields, string? FiltersJson);

public interface IReportDefinitionValidator
{
    public ReportDefinition Parse(string? json);
    public string BuildJson(string source, string fieldsCsv, string? filtersJson);
    public IReadOnlyDictionary<string, string[]> GetAvailableSources();
}


