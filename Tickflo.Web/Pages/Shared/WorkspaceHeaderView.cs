namespace Tickflo.Web.Pages.Shared;

public class WorkspaceHeaderView
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = "Workspace";
    public string? SectionTitle { get; set; }
    public string? NewActionLabel { get; set; }
    public string? NewActionHref { get; set; }
}
