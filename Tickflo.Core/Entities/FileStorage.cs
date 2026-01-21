namespace Tickflo.Core.Entities;

/// <summary>
/// Represents a file stored in RustFS, with metadata for tracking and management.
/// </summary>
public class FileStorage
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? UserId { get; set; }
    public string Path { get; set; } = string.Empty; // Relative path in RustFS
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; } // Size in bytes
    public string FileType { get; set; } = string.Empty; // "image", "document", "avatar", "logo", "banner"
    public string Category { get; set; } = string.Empty; // "user-avatar", "workspace-logo", "ticket-attachment", etc.
    public string? Description { get; set; }
    public string PublicUrl { get; set; } = string.Empty; // Full URL to access the file
    public bool IsPublic { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }
    public string? Metadata { get; set; } // JSON for additional metadata (width, height for images, etc.)

    // Related entities
    public int? TicketId { get; set; }
    public int? ContactId { get; set; }
    public string? RelatedEntityType { get; set; } // "Ticket", "Contact", "Workspace", etc.
    public int? RelatedEntityId { get; set; }
}
