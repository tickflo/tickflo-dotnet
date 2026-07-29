namespace Tickflo.Core.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the Mailgun inbound email webhook payload.
/// Mailgun sends multipart/form-data; these fields are parsed from the POST body.
/// </summary>
public class InboundEmailPayload
{
    // --- Core email fields ---

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body-plain")]
    public string BodyPlain { get; set; } = string.Empty;

    [JsonPropertyName("body-html")]
    public string? BodyHtml { get; set; }

    [JsonPropertyName("stripped-text")]
    public string? StrippedText { get; set; }

    [JsonPropertyName("stripped-html")]
    public string? StrippedHtml { get; set; }

    [JsonPropertyName("stripped-signature")]
    public string? StrippedSignature { get; set; }

    // --- Recipient info ---

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = string.Empty;

    [JsonPropertyName("To")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("Cc")]
    public string? Cc { get; set; }

    // --- Message identification ---

    [JsonPropertyName("message-id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("Message-Id")]
    public string MessageIdAlt { get; set; } = string.Empty;

    [JsonPropertyName("message-headers")]
    public string? MessageHeaders { get; set; }

    // --- Reply tracking ---

    [JsonPropertyName("In-Reply-To")]
    public string? InReplyTo { get; set; }

    [JsonPropertyName("References")]
    public string? References { get; set; }

    // --- Attachments ---

    [JsonPropertyName("attachment-count")]
    public int AttachmentCount { get; set; }

    [JsonPropertyName("attachment-info")]
    public string? AttachmentInfo { get; set; }

    /// <summary>
    /// Individual attachment entries are sent as attachment-N fields.
    /// We parse them from the form data; this is a convenience lookup.
    /// </summary>
    [JsonIgnore]
    public List<MailgunAttachment> Attachments { get; set; } = [];

    // --- Sender info ---

    [JsonPropertyName("from-name")]
    public string? FromName { get; set; }

    // --- Timestamp ---

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("Date")]
    public string? Date { get; set; }

    // --- Mailgun signature ---

    [JsonPropertyName("signature")]
    public MailgunSignature? Signature { get; set; }

    public string GetMessageId()
        => !string.IsNullOrWhiteSpace(this.MessageId) ? this.MessageId : this.MessageIdAlt;

    public string GetSenderEmail()
        => !string.IsNullOrWhiteSpace(this.Sender) ? this.Sender : this.From;

    /// <summary>
    /// Extracts the local part (before @) from the recipient address.
    /// </summary>
    public string GetRecipientLocalPart()
    {
        var addr = !string.IsNullOrWhiteSpace(this.Recipient) ? this.Recipient : this.To;
        var atIndex = addr.IndexOf('@');
        return atIndex > 0 ? addr[..atIndex].ToLowerInvariant() : string.Empty;
    }
}

/// <summary>
/// Mailgun signature for webhook verification.
/// </summary>
public class MailgunSignature
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Represents a file attachment from Mailgun.
/// Mailgun sends attachments as multipart form-data fields named "attachment-N".
/// Each has a URL for temporary download, filename, content-type, and size.
/// </summary>
public class MailgunAttachment
{
    /// <summary>
    /// The temporary Mailgun URL to download the attachment content.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Original filename.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Whether this attachment should be rejected (too large, wrong type, etc.).
    /// </summary>
    public bool IsRejected { get; set; }

    /// <summary>
    /// Reason for rejection, if applicable.
    /// </summary>
    public string? RejectionReason { get; set; }
}
