namespace Tickflo.Core.Entities;

/// <summary>
/// Represents a comment on a ticket with support for dual visibility (client-visible or internal-only).
/// Maintains full audit trail with creator and editor information.
/// </summary>
public class TicketComment
{
    /// <summary>
    /// Unique identifier for the comment within the system.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The workspace ID this comment belongs to (for scoping and security).
    /// </summary>
    public int WorkspaceId { get; set; }

    /// <summary>
    /// The ticket ID this comment is attached to.
    /// </summary>
    public int TicketId { get; set; }

    /// <summary>
    /// The user ID of the person who created this comment (for audit trail).
    /// </summary>
    public int CreatedByUserId { get; set; }

    /// <summary>
    /// The comment text content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Controls visibility: true means visible in client portal, false means internal-only.
    /// </summary>
    public bool IsVisibleToClient { get; set; } = false;

    /// <summary>
    /// Timestamp when the comment was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the comment was last updated (UTC), null if never updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The user ID of the person who last updated this comment, null if never updated.
    /// </summary>
    public int? UpdatedByUserId { get; set; }
    
    // Navigation properties

    /// <summary>
    /// Navigation property: The ticket this comment belongs to.
    /// </summary>
    public Ticket? Ticket { get; set; }

    /// <summary>
    /// Navigation property: The user who created this comment.
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Navigation property: The user who last updated this comment.
    /// </summary>
    public User? UpdatedByUser { get; set; }
}
