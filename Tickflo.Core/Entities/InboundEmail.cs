namespace Tickflo.Core.Entities;

/// <summary>
/// Records an inbound email received via Mailgun webhook.
/// Each email is processed into a ticket; the record preserves the raw payload
/// for audit and debugging purposes.
/// </summary>
public class InboundEmail
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int RouteId { get; set; }

    /// <summary>
    /// The sender's email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// The sender's display name, if provided.
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// The recipient email address that received this email (matches a route's LocalPart).
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// The subject line of the inbound email.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// The plain-text body of the email.
    /// </summary>
    public string BodyPlain { get; set; } = string.Empty;

    /// <summary>
    /// The HTML body of the email, if present.
    /// </summary>
    public string? BodyHtml { get; set; }

    /// <summary>
    /// The Mailgun message ID (Message-Id header) for deduplication.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the original inbound email if this is a reply/forward.
    /// Used to track conversation threading.
    /// </summary>
    public int? InReplyToEmailId { get; set; }

    /// <summary>
    /// The ticket created from this email, if processing completed.
    /// </summary>
    public int? TicketId { get; set; }

    /// <summary>
    /// The contact identified from the sender's email, if found.
    /// </summary>
    public int? ContactId { get; set; }

    /// <summary>
    /// Processing status: Pending, Processed, Failed, Rejected.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Raw Mailgun payload stored for debugging.
    /// </summary>
    public string? RawPayload { get; set; }

    /// <summary>
    /// Timestamps for the full lifecycle.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public InboundEmailRoute Route { get; set; } = null!;
    public ICollection<InboundEmailAttachment> Attachments { get; set; } = [];
}
