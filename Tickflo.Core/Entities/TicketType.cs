namespace Tickflo.Core.Entities;

public class TicketType
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "neutral";
    public int SortOrder { get; set; }
}
