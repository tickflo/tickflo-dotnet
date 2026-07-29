namespace Tickflo.Core.Entities;

/// <summary>
/// Represents a file attached to an inbound email.
/// Attachments are downloaded from Mailgun's temp URLs and stored in RustFS.
/// </summary>
public class InboundEmailAttachment
{
    public int Id { get; set; }
    public int InboundEmailId { get; set; }
    public int WorkspaceId { get; set; }

    /// <summary>
    /// Original filename from the email attachment.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type of the attachment.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The Mailgun temporary URL used to download the attachment content.
    /// Saved for reprocessing if needed.
    /// </summary>
    public string? MailgunUrl { get; set; }

    /// <summary>
    /// The RustFS path where the attachment was stored after processing.
    /// Null until the attachment has been downloaded and stored.
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// The public URL to access the stored attachment.
    /// Null until the attachment has been processed.
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// Whether the attachment has been successfully stored in RustFS.
    /// </summary>
    public bool IsStored { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public InboundEmail InboundEmail { get; set; } = null!;
}
