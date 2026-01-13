namespace Tickflo.Core.Entities;

public class TicketStatus
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    // DaisyUI color keyword: primary, secondary, accent, info, success, warning, error, neutral
    public string Color { get; set; } = "neutral";
    public int SortOrder { get; set; } = 0;
    public bool IsClosedState { get; set; } = false;
}
