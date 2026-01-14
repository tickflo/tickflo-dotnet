namespace Tickflo.Core.Entities;

public class Ticket : IWorkspaceEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ContactId { get; set; }
    public int? LocationId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Standard";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "New";
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Multi-inventory support
    public ICollection<TicketInventory> TicketInventories { get; set; } = new List<TicketInventory>();
    
    // Comments
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}