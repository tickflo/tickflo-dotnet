namespace Tickflo.Web.Pages.Shared;

public class FilterBarView
{
    public string QueryParamName { get; set; } = "Query";
    public string? QueryValue { get; set; }
    public string Placeholder { get; set; } = "Search";
    public string SubmitLabel { get; set; } = "Filter";
    public List<FilterSelect> Selects { get; set; } = [];
}

public class FilterSelect
{
    public string Name { get; set; } = string.Empty;
    public List<FilterOption> Options { get; set; } = [];
    public string? CssClass { get; set; }
}

public class FilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
