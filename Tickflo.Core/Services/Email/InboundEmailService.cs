namespace Tickflo.Core.Services.Email;

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.DTOs;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Storage;
using Tickflo.Core.Services.Tickets;

/// <inheritdoc />
public class InboundEmailService(
    TickfloDbContext dbContext,
    TickfloConfig config,
    IInboundEmailHMACValidator hmacValidator,
    IEmailSendService emailSendService,
    ITicketCreationService ticketCreationService,
    IFileStorageService fileStorageService,
    ILogger<InboundEmailService> logger) : IInboundEmailService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly TickfloConfig config = config;
    private readonly IInboundEmailHMACValidator hmacValidator = hmacValidator;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly ITicketCreationService ticketCreationService = ticketCreationService;
    private readonly IFileStorageService fileStorageService = fileStorageService;
    private readonly ILogger<InboundEmailService> logger = logger;
    private const int SystemUserId = 1; // System-level user for automated operations

    /// <inheritdoc />
    public async Task<InboundEmailResult> ProcessAsync(
        InboundEmailPayload payload,
        Dictionary<string, (Stream Stream, string ContentType, long Size)>? attachmentStreams,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate HMAC signature
        if (!this.ValidateSignature(payload))
        {
            return InboundEmailResult.FailedResult(0, "Invalid HMAC signature");
        }

        // Step 2: Deduplicate by MessageId
        var messageId = payload.GetMessageId();
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return InboundEmailResult.FailedResult(0, "No message ID in payload");
        }

        var existing = await this.dbContext.InboundEmails
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);

        if (existing != null)
        {
            return InboundEmailResult.DuplicateResult($"Email {messageId} already processed");
        }

        // Step 3: Resolve workspace route by recipient local part
        var localPart = payload.GetRecipientLocalPart();
        if (string.IsNullOrWhiteSpace(localPart))
        {
            return InboundEmailResult.FailedResult(0, "Could not determine recipient local part");
        }

        var route = await this.dbContext.InboundEmailRoutes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.LocalPart == localPart && r.Active, cancellationToken);

        if (route == null)
        {
            this.logger.LogWarning("No active route found for local part: {LocalPart}", localPart);
            return InboundEmailResult.FailedResult(0, $"No active route for {localPart}@{config.InboundEmail.Domain}");
        }

        // Step 4: Create InboundEmail record
        var inboundEmail = new InboundEmail
        {
            WorkspaceId = route.WorkspaceId,
            RouteId = route.Id,
            FromEmail = payload.GetSenderEmail(),
            FromName = payload.FromName,
            ToEmail = $"{localPart}@{config.InboundEmail.Domain}",
            Subject = payload.Subject ?? "(no subject)",
            BodyPlain = payload.BodyPlain ?? payload.StrippedText ?? string.Empty,
            BodyHtml = payload.BodyHtml ?? payload.StrippedHtml,
            MessageId = messageId,
            InReplyToEmailId = null, // Reply detection done below
            Status = "Pending",
            RawPayload = System.Text.Json.JsonSerializer.Serialize(payload),
            ReceivedAt = DateTime.UtcNow,
        };

        // Step 5: Reply detection
        if (!string.IsNullOrWhiteSpace(payload.InReplyTo))
        {
            var parentMessageId = ExtractMessageId(payload.InReplyTo);
            var parentEmail = await this.dbContext.InboundEmails
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.MessageId == parentMessageId, cancellationToken);

            if (parentEmail != null)
            {
                inboundEmail.InReplyToEmailId = parentEmail.Id;
            }
        }

        this.dbContext.InboundEmails.Add(inboundEmail);
        await this.dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Step 6: Find or create contact
            var contact = await this.ResolveContactAsync(inboundEmail, cancellationToken);

            // Step 7: Store attachments
            var storedAttachments = await this.ProcessAttachmentsAsync(
                inboundEmail, route.WorkspaceId, payload, attachmentStreams, cancellationToken);

            // Step 8: Create ticket
            var ticket = await this.CreateTicketAsync(
                route, inboundEmail, contact, cancellationToken);

            // Step 9: Update inbound email with success
            inboundEmail.TicketId = ticket.Id;
            inboundEmail.ContactId = contact?.Id;
            inboundEmail.Status = "Processed";
            inboundEmail.ProcessedAt = DateTime.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            // Step 10: Link attachments to ticket
            if (storedAttachments.Count > 0)
            {
                await this.LinkAttachmentsToTicketAsync(storedAttachments, ticket.Id, route.WorkspaceId, cancellationToken);
            }

            // Step 11: Send confirmation email (fire-and-forget; failures are logged, not thrown)
            await this.SendConfirmationAsync(inboundEmail, contact, route, cancellationToken);

            this.logger.LogInformation(
                "Inbound email {EmailId} processed into ticket {TicketId}",
                inboundEmail.Id,
                ticket.Id);

            return InboundEmailResult.SuccessResult(inboundEmail.Id, ticket.Id);
        }
        catch (Exception ex)
        {
            inboundEmail.Status = "Failed";
            inboundEmail.ErrorMessage = ex.Message;
            await this.dbContext.SaveChangesAsync(cancellationToken);

            this.logger.LogError(ex,
                "Failed to process inbound email {EmailId}: {Message}",
                inboundEmail.Id, ex.Message);

            return InboundEmailResult.FailedResult(inboundEmail.Id, ex.Message);
        }
    }

    private bool ValidateSignature(InboundEmailPayload payload)
    {
        if (payload.Signature == null)
        {
            this.logger.LogWarning("Mailgun webhook missing signature block");
            return false;
        }

        var signingKey = config.InboundEmail.WebhookSigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            this.logger.LogWarning("InboundEmail:WebhookSigningKey is not configured — skipping HMAC validation");
            return true; // Allow when not configured (dev mode)
        }

        return hmacValidator.Validate(
            payload.Signature.Timestamp,
            payload.Signature.Token,
            payload.Signature.Signature,
            signingKey);
    }

    private async Task<Contact?> ResolveContactAsync(InboundEmail inboundEmail, CancellationToken ct)
    {
        var email = inboundEmail.FromEmail.Trim().ToLowerInvariant();

        // Look for an existing contact with this email
        var contact = await this.dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email
                && c.WorkspaceId == inboundEmail.WorkspaceId, ct);

        if (contact != null)
        {
            return contact;
        }

        // Create a new contact from the sender info
        var displayName = inboundEmail.FromName ?? inboundEmail.FromEmail;
        contact = new Contact
        {
            WorkspaceId = inboundEmail.WorkspaceId,
            Name = displayName,
            Email = inboundEmail.FromEmail,
            CreatedAt = DateTime.UtcNow,
        };

        this.dbContext.Contacts.Add(contact);
        await this.dbContext.SaveChangesAsync(ct);

        this.logger.LogInformation("Created contact {ContactId} from inbound email: {Email}", contact.Id, email);
        return contact;
    }

    private async Task<List<InboundEmailAttachment>> ProcessAttachmentsAsync(
        InboundEmail inboundEmail,
        int workspaceId,
        InboundEmailPayload payload,
        Dictionary<string, (Stream Stream, string ContentType, long Size)>? attachmentStreams,
        CancellationToken ct)
    {
        var stored = new List<InboundEmailAttachment>();

        if (attachmentStreams == null || attachmentStreams.Count == 0)
        {
            return stored;
        }

        var maxSize = config.InboundEmail.MaxAttachmentSize;
        var allowedTypes = config.InboundEmail.AllowedMimeTypes?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToHashSet() ?? [];

        foreach (var (fileName, (stream, contentType, size)) in attachmentStreams)
        {
            var attachment = new InboundEmailAttachment
            {
                InboundEmailId = inboundEmail.Id,
                WorkspaceId = workspaceId,
                FileName = fileName,
                ContentType = contentType,
                Size = size,
                MailgunUrl = null, // Content is in the request stream, not a URL
                CreatedAt = DateTime.UtcNow,
            };

            // Validate size
            if (size > maxSize)
            {
                this.logger.LogWarning(
                    "Attachment {FileName} ({Size} bytes) exceeds max size ({MaxSize})",
                    fileName, size, maxSize);
                attachment.IsStored = false;
                this.dbContext.InboundEmailAttachments.Add(attachment);
                await this.dbContext.SaveChangesAsync(ct);
                stored.Add(attachment);
                continue;
            }

            // Validate MIME type (if allowlist is configured)
            if (allowedTypes.Count > 0 && !allowedTypes.Contains(contentType.ToLowerInvariant()))
            {
                this.logger.LogWarning(
                    "Attachment {FileName} has disallowed content type: {ContentType}",
                    fileName, contentType);
                attachment.IsStored = false;
                this.dbContext.InboundEmailAttachments.Add(attachment);
                await this.dbContext.SaveChangesAsync(ct);
                stored.Add(attachment);
                continue;
            }

            // Store to RustFS
            try
            {
                var storagePath = $"inbound/{workspaceId}/{inboundEmail.Id}/{Guid.NewGuid():N}_{fileName}";
                var publicUrl = await fileStorageService.UploadFileAsync(storagePath, stream, contentType);

                attachment.StoragePath = storagePath;
                attachment.PublicUrl = publicUrl;
                attachment.IsStored = true;

                this.dbContext.InboundEmailAttachments.Add(attachment);
                await this.dbContext.SaveChangesAsync(ct);
                stored.Add(attachment);

                this.logger.LogInformation(
                    "Stored attachment {FileName} → {StoragePath}", fileName, storagePath);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to store attachment {FileName}", fileName);

                attachment.IsStored = false;
                this.dbContext.InboundEmailAttachments.Add(attachment);
                await this.dbContext.SaveChangesAsync(ct);
                stored.Add(attachment);
            }
        }

        return stored;
    }

    private async Task<Ticket> CreateTicketAsync(
        InboundEmailRoute route,
        InboundEmail inboundEmail,
        Contact? contact,
        CancellationToken ct)
    {
        var request = new TicketCreationRequest
        {
            Subject = inboundEmail.Subject,
            Description = inboundEmail.BodyPlain,
            Type = route.DefaultTicketType,
            Priority = route.DefaultTicketPriority,
            LocationId = route.DefaultLocationId,
            ContactId = contact?.Id,
        };

        return await ticketCreationService.CreateTicketAsync(
            route.WorkspaceId,
            request,
            SystemUserId);
    }

    private async Task LinkAttachmentsToTicketAsync(
        List<InboundEmailAttachment> attachments,
        int ticketId,
        int workspaceId,
        CancellationToken ct)
    {
        var storedFiles = attachments
            .Where(a => a.IsStored && !string.IsNullOrWhiteSpace(a.PublicUrl))
            .ToList();

        if (storedFiles.Count == 0)
        {
            return;
        }

        var fileRecords = storedFiles.Select(a => new FileStorage
        {
            WorkspaceId = workspaceId,
            UserId = SystemUserId,
            FileName = a.FileName,
            ContentType = a.ContentType,
            Size = a.Size,
            FileType = a.ContentType,
            Path = a.StoragePath ?? string.Empty,
            PublicUrl = a.PublicUrl,
            IsPublic = false,
            IsArchived = false,
            TicketId = ticketId,
            RelatedEntityType = "InboundEmailAttachment",
            RelatedEntityId = a.Id,
            Category = "email-attachment",
            Description = $"Attachment from inbound email #{a.InboundEmailId}",
            CreatedAt = DateTime.UtcNow,
        });

        this.dbContext.FileStorages.AddRange(fileRecords);
        await this.dbContext.SaveChangesAsync(ct);
    }

    private async Task SendConfirmationAsync(
        InboundEmail inboundEmail,
        Contact? contact,
        InboundEmailRoute route,
        CancellationToken ct)
    {
        try
        {
            var variables = new Dictionary<string, string>
            {
                ["ticket_id"] = inboundEmail.TicketId?.ToString(CultureInfo.InvariantCulture) ?? "?",
                ["subject"] = inboundEmail.Subject,
                ["contact_name"] = inboundEmail.FromName ?? inboundEmail.FromEmail,
            };

            await emailSendService.AddToQueueAsync(
                inboundEmail.FromEmail,
                EmailTemplateType.TicketReceived,
                variables,
                sentByUserId: SystemUserId);
        }
        catch (Exception ex)
        {
            // Confirmation is best-effort; log failure but don't fail the whole pipeline
            this.logger.LogWarning(ex,
                "Failed to queue confirmation email for inbound email {EmailId}: {Message}",
                inboundEmail.Id, ex.Message);
        }
    }

    /// <summary>
    /// Extracts a clean Message-ID from an In-Reply-To or References header value.
    /// Handles angle brackets, whitespace, and multi-reference lists.
    /// </summary>
    private static string? ExtractMessageId(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var trimmed = reference.Trim();
        if (trimmed.StartsWith('<'))
        {
            var end = trimmed.IndexOf('>');
            return end > 1 ? trimmed[1..end] : trimmed;
        }

        // If there are multiple references, take the first one
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex > 0)
        {
            var first = trimmed[..spaceIndex];
            return first.StartsWith('<') ? first[1..^1] : first;
        }

        return trimmed;
    }
}
