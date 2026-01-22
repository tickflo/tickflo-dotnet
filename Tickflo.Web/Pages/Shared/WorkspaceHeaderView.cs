#pragma warning disable CA1716
namespace Tickflo.Web.Pages.Shared;
#pragma warning restore CA1716

public class WorkspaceHeaderView
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = "Workspace";
    public string? SectionTitle { get; set; }
    public string? NewActionLabel { get; set; }
    public string? NewActionHref { get; set; }
    public bool AllowNewAction { get; set; } = true;
}
