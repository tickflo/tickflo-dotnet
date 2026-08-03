namespace Tickflo.Web.Controllers;

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tickflo.Core.Data;
using Tickflo.Core.DTOs;
using Tickflo.Core.Exceptions;
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
    TickfloDbContext dbContext,
    ILogger<InboundEmailController> logger) : Controller
{
    private readonly IInboundEmailService inboundEmailService = inboundEmailService;
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly ILogger<InboundEmailController> logger = logger;

    /// <summary>
    /// Mailgun forwards inbound emails here via POST with multipart/form-data.
    /// The body contains email fields (from, subject, body-plain, etc.),
    /// signature fields (timestamp, token, signature), and file attachments.
    /// Always returns 200 per Mailgun spec — errors are persisted on the record,
    /// not returned to the caller to prevent Mailgun retries on expected failures.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        InboundEmailPayload payload;
        Dictionary<string, (Stream Stream, string ContentType, long Size)>? attachmentStreams;

        try
        {
            payload = this.ParsePayload();
            attachmentStreams = this.ExtractAttachments();
        }
        catch (Exception ex)
        {
            // Payload parsing failure — nothing to persist, just log
            this.logger.LogError(ex, "Failed to parse inbound email payload: {Message}", ex.Message);
            return this.Ok();
        }

        InboundEmailResult result;

        try
        {
            result = await this.inboundEmailService.ProcessAsync(payload, attachmentStreams);
        }
        catch (HttpException ex)
        {
            // Expected validation failure (HMAC, unknown route, etc.)
            // The service creates the InboundEmail record early; update it to Failed
            this.logger.LogWarning("Inbound email rejected: {Message}", ex.Message);
            await this.MarkEmailFailedByMessageId(payload.GetMessageId(), ex.Message);
            return this.Ok();
        }
        catch (Exception ex)
        {
            // Unexpected pipeline failure
            this.logger.LogError(ex, "Failed to process inbound email: {Message}", ex.Message);
            await this.MarkEmailFailedByMessageId(payload.GetMessageId(), ex.Message);
            return this.Ok();
        }

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

        return this.Ok();
    }

    private InboundEmailPayload ParsePayload()
    {
        var form = this.Request.Form;

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
            To = form["To"].FirstOrDefault(),
            MessageId = form["message-id"].FirstOrDefault() ?? string.Empty,
            MessageHeaders = form["message-headers"].FirstOrDefault(),
            FromName = form["from-name"].FirstOrDefault(),
            Timestamp = long.TryParse(form["timestamp"].FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts) ? ts : 0,
            Date = form["Date"].FirstOrDefault(),

            // Attachment info
            AttachmentCount = int.TryParse(form["attachment-count"].FirstOrDefault(), out var count) ? count : 0,
        };

        // Parse Mailgun signature
        var signature = new MailgunSignature
        {
            Timestamp = form["signature[timestamp]"].FirstOrDefault() ?? string.Empty,
            Token = form["signature[token]"].FirstOrDefault() ?? string.Empty,
            Signature = form["signature[signature]"].FirstOrDefault() ?? string.Empty,
        };

        // If structured signature wasn't found, try top-level fields
        if (string.IsNullOrWhiteSpace(signature.Timestamp))
        {
            signature.Timestamp = form["timestamp"].FirstOrDefault() ?? string.Empty;
            signature.Token = form["token"].FirstOrDefault() ?? string.Empty;
            signature.Signature = form["signature"].FirstOrDefault() ?? string.Empty;
        }

        payload.Signature = signature;

        // Parse In-Reply-To from message-headers for reply detection
        payload.InReplyTo = InboundEmailPayload.ExtractHeaderValue(
            payload.MessageHeaders, "In-Reply-To");

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
    /// Marks the InboundEmail record as Failed when the pipeline throws after record creation.
    /// Looks up the record by its MessageId (set early in the pipeline).
    /// </summary>
    private async Task MarkEmailFailedByMessageId(string messageId, string error)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return;
        }

        try
        {
            var email = await this.dbContext.InboundEmails
                .FirstOrDefaultAsync(e => e.MessageId == messageId);

            if (email != null && email.Status == "Pending")
            {
                email.Status = "Failed";
                email.ErrorMessage = error;
                await this.dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to mark inbound email as Failed: {Message}", ex.Message);
        }
    }
}
