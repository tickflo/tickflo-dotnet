namespace Tickflo.Web.Controllers;

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tickflo.Core.DTOs;
using Tickflo.Core.Services.Email;

/// <summary>
/// Webhook endpoint for Mailgun inbound email routing.
/// Receives forwarded emails as multipart/form-data POSTs,
/// validates the HMAC signature, processes the email pipeline,
/// and always returns 200 to acknowledge receipt.
/// </summary>
[AllowAnonymous]
[Route("api/inbound-email")]
public class InboundEmailController(
    IInboundEmailService inboundEmailService,
    ILogger<InboundEmailController> logger) : Controller
{
    private readonly IInboundEmailService inboundEmailService = inboundEmailService;
    private readonly ILogger<InboundEmailController> logger = logger;

    /// <summary>
    /// Mailgun forwards inbound emails here via POST with multipart/form-data.
    /// The body contains email fields (from, subject, body-plain, etc.),
    /// signature fields (timestamp, token, signature), and file attachments.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Receive()
    {
        try
        {
            var payload = await this.ParsePayloadAsync();
            var attachmentStreams = this.ExtractAttachments();

            var result = await this.inboundEmailService.ProcessAsync(payload, attachmentStreams);

            if (result.Success)
            {
                this.logger.LogInformation(
                    "Inbound email {EmailId} processed → ticket {TicketId}",
                    result.InboundEmailId,
                    result.TicketId);
            }
            else
            {
                this.logger.LogWarning(
                    "Inbound email processing result: {Status} — {Message}",
                    result.Status,
                    result.Message);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to process inbound email webhook: {Message}", ex.Message);
        }

        // Mailgun expects 200 to acknowledge receipt; non-200 triggers retry.
        // Processing errors are persisted in the InboundEmail record, not returned.
        return this.Ok();
    }

    private async Task<InboundEmailPayload> ParsePayloadAsync()
    {
        var request = this.Request;
        var form = await request.ReadFormAsync();

        var payload = new InboundEmailPayload
        {
            From = form["from"].FirstOrDefault() ?? string.Empty,
            Sender = form["sender"].FirstOrDefault() ?? string.Empty,
            Subject = form["subject"].FirstOrDefault() ?? string.Empty,
            BodyPlain = form["body-plain"].FirstOrDefault() ?? string.Empty,
            BodyHtml = form["body-html"].FirstOrDefault(),
            StrippedText = form["stripped-text"].FirstOrDefault(),
            StrippedHtml = form["stripped-html"].FirstOrDefault(),
            StrippedSignature = form["stripped-signature"].FirstOrDefault(),
            Recipient = form["recipient"].FirstOrDefault() ?? string.Empty,
            To = form["To"].FirstOrDefault() ?? string.Empty,
            Cc = form["Cc"].FirstOrDefault(),
            MessageId = form["message-id"].FirstOrDefault() ?? string.Empty,
            MessageIdAlt = form["Message-Id"].FirstOrDefault() ?? string.Empty,
            MessageHeaders = form["message-headers"].FirstOrDefault(),
            InReplyTo = form["In-Reply-To"].FirstOrDefault(),
            References = form["References"].FirstOrDefault(),
            FromName = form["from-name"].FirstOrDefault(),
            Timestamp = long.TryParse(form["timestamp"].FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts) ? ts : 0,
            Date = form["Date"].FirstOrDefault(),

            // Attachment info
            AttachmentCount = int.TryParse(form["attachment-count"].FirstOrDefault(), out var count) ? count : 0,
            AttachmentInfo = form["attachment-info"].FirstOrDefault(),
        };

        // Parse Mailgun signature
        var signature = new MailgunSignature
        {
            Timestamp = form["signature[timestamp]"].FirstOrDefault() ?? string.Empty,
            Token = form["signature[token]"].FirstOrDefault() ?? string.Empty,
            Signature = form["signature[signature]"].FirstOrDefault() ?? string.Empty,

            // Also try flattened format: Mailgun may send as top-level fields
        };

        // If structured signature wasn't found, try top-level fields
        if (string.IsNullOrWhiteSpace(signature.Timestamp))
        {
            signature.Timestamp = form["timestamp"].FirstOrDefault() ?? string.Empty;
            signature.Token = form["token"].FirstOrDefault() ?? string.Empty;
            signature.Signature = form["signature"].FirstOrDefault() ?? string.Empty;
        }

        payload.Signature = signature;

        // Parse attachment metadata if available
        if (!string.IsNullOrWhiteSpace(payload.AttachmentInfo))
        {
            try
            {
                var attachmentDict = JsonSerializer.Deserialize<Dictionary<string, MailgunAttachmentJson>>(payload.AttachmentInfo);
                if (attachmentDict != null)
                {
                    foreach (var (key, value) in attachmentDict)
                    {
                        payload.Attachments.Add(new MailgunAttachment
                        {
                            Name = value.Name,
                            ContentType = value.ContentType,
                            Size = value.Size,
                            Url = value.Url,
                        });
                    }
                }
            }
            catch (JsonException)
            {
                // attachment-info is best-effort; log and continue
                this.logger.LogWarning("Failed to parse attachment-info JSON");
            }
        }

        return payload;
    }

    private Dictionary<string, (Stream Stream, string ContentType, long Size)>? ExtractAttachments()
    {
        if (this.Request.Form.Files.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, (Stream, string, long)>();

        foreach (var file in this.Request.Form.Files)
        {
            // Mailgun sends attachments as "attachment-1", "attachment-2", etc.
            var stream = new MemoryStream();
            file.CopyTo(stream);
            stream.Position = 0;

            result[file.FileName] = (stream, file.ContentType, file.Length);
        }

        return result;
    }

    /// <summary>
    /// JSON structure from the attachment-info field for parsing.
    /// </summary>
    private sealed class MailgunAttachmentJson
    {
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
