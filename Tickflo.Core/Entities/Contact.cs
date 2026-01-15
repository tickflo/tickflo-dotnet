namespace Tickflo.Core.Entities;

public class Contact
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
    public string? PreferredChannel { get; set; } // email, phone, chat
    public string? Priority { get; set; } // Low, Normal, High, Urgent
    public string? Status { get; set; } // Active, Archived
    public int? AssignedUserId { get; set; }
    public DateTime? LastInteraction { get; set; }
    public DateTime CreatedAt { get; set; }
}