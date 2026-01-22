#pragma warning disable CA1716
namespace Tickflo.Web.Pages.Shared;
#pragma warning restore CA1716

public class ActionRowView
{
    public List<ActionItem> Actions { get; set; } = [];
}

public class ActionItem
{
    public string Label { get; set; } = string.Empty;
    public string? Href { get; set; }
    public bool Disabled { get; set; }
    public string ButtonClass { get; set; } = "btn btn-soft";
}
