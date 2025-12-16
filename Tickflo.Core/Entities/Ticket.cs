namespace Tickflo.Core.Entities;

public class Ticket
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int ContactId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "New";
    public int? AssignedUserId { get; set; }
    public string? InventoryRef { get; set; } // SKU or item reference
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}