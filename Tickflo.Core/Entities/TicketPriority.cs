using System.ComponentModel.DataAnnotations.Schema;

namespace Tickflo.Core.Entities;

[Table("priorities")]
public class TicketPriority
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "neutral";
    public int SortOrder { get; set; } = 0;
}
