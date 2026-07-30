namespace Tickflo.Core.DTOs;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the Mailgun inbound email webhook payload.
/// Mailgun sends multipart/form-data; these fields are parsed from the POST body.
/// Only documented Mailgun parameters are included here.
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

    /// <summary>
    /// The To header value from the original email (MIME header posted by Mailgun).
    /// Routing uses <see cref="Recipient"/> instead; this is informational.
    /// </summary>
    [JsonPropertyName("To")]
    public string? To { get; set; }

    // --- Message identification ---

    [JsonPropertyName("message-id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("message-headers")]
    public string? MessageHeaders { get; set; }

    // --- Attachments ---

    [JsonPropertyName("attachment-count")]
    public int AttachmentCount { get; set; }

    // --- Sender info ---

    /// <summary>
    /// Best-effort display name extracted by Mailgun from the From header.
    /// Not guaranteed to be present; the service falls back to the email address.
    /// </summary>
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

    public string GetMessageId() => this.MessageId;

    public string GetSenderEmail()
        => !string.IsNullOrWhiteSpace(this.Sender) ? this.Sender : this.From;

    /// <summary>
    /// Extracts the local part (before @) from the recipient address.
    /// </summary>
    public string GetRecipientLocalPart()
    {
        var addr = !string.IsNullOrWhiteSpace(this.Recipient) ? this.Recipient : this.To;
        var atIndex = addr?.IndexOf('@') ?? -1;
        return atIndex > 0 ? addr![..atIndex].ToLowerInvariant() : string.Empty;
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
