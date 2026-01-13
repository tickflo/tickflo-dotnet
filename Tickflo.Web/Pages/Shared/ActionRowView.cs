namespace Tickflo.Web.Pages.Shared;

public class ActionRowView
{
    public List<ActionItem> Actions { get; set; } = new();
}

public class ActionItem
{
    public string Label { get; set; } = string.Empty;
    public string? Href { get; set; }
    public bool Disabled { get; set; }
    public string ButtonClass { get; set; } = "btn btn-soft";
}
