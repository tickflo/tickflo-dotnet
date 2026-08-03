namespace Tickflo.Core.Services.Email;

using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.DTOs;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Storage;
using Tickflo.Core.Services.Tickets;

/// <inheritdoc />
public class InboundEmailService(
    TickfloDbContext dbContext,
    TickfloConfig config,
    IInboundEmailHMACValidator hmacValidator,
    IEmailSendService emailSendService,
    ITicketCreationService ticketCreationService,
    ITicketCommentService ticketCommentService,
    IFileStorageService fileStorageService,
    ILogger<InboundEmailService> logger) : IInboundEmailService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly TickfloConfig config = config;
    private readonly IInboundEmailHMACValidator hmacValidator = hmacValidator;
    private readonly IEmailSendService emailSendService = emailSendService;
    private readonly ITicketCreationService ticketCreationService = ticketCreationService;
    private readonly ITicketCommentService ticketCommentService = ticketCommentService;
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
        this.ValidateSignature(payload);

        // Step 2: Deduplicate by MessageId
        var messageId = payload.GetMessageId();
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new BadRequestException("No message ID in payload");
        }

        var existing = await this.dbContext.InboundEmails
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.MessageId == messageId, cancellationToken);

        if (existing != null)
        {
            return InboundEmailResult.DuplicateResult($"Email {messageId} already processed");
        }

        // Step 3: Resolve workspace route by recipient local part
        var route = await this.ResolveRouteAsync(payload);

        // Step 4: Create InboundEmail record
        var inboundEmail = await this.CreateInboundEmailRecordAsync(payload, route, cancellationToken);

        // Step 5: Find or create contact
        var contact = await this.ResolveContactAsync(inboundEmail, cancellationToken);

        // Step 6: Validate and store attachments
        var storedAttachments = await this.ProcessAttachmentsAsync(
            inboundEmail, route.WorkspaceId, attachmentStreams, cancellationToken);

        // Step 7: Try to resolve this as a reply to an existing ticket
        var replyTicket = await this.TryResolveReplyTicketAsync(payload, route, contact, cancellationToken);

        if (replyTicket != null)
        {
            // This email is a reply — add comment on the existing ticket
            await this.HandleReplyAsync(inboundEmail, replyTicket, contact, storedAttachments, cancellationToken);

            this.logger.LogInformation(
                "Inbound email {EmailId} processed as reply → ticket {TicketId}",
                inboundEmail.Id,
                replyTicket.Id);

            return InboundEmailResult.SuccessResult(inboundEmail.Id, replyTicket.Id);
        }

        // Step 8: Create new ticket (no matching thread found)
        var ticket = await this.CreateTicketAsync(route, inboundEmail, contact);

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
        await this.SendConfirmationAsync(inboundEmail);

        this.logger.LogInformation(
            "Inbound email {EmailId} processed into ticket {TicketId}",
            inboundEmail.Id,
            ticket.Id);

        return InboundEmailResult.SuccessResult(inboundEmail.Id, ticket.Id);
    }

    /// <summary>
    /// Validates the Mailgun HMAC signature. Throws if invalid when signing key is configured.
    /// </summary>
    private void ValidateSignature(InboundEmailPayload payload)
    {
        if (payload.Signature == null)
        {
            throw new BadRequestException("Mailgun webhook missing signature block");
        }

        var signingKey = this.config.InboundEmail.WebhookSigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            this.logger.LogWarning("InboundEmail:WebhookSigningKey is not configured — skipping HMAC validation");
            return;
        }

        if (!this.hmacValidator.Validate(
                payload.Signature.Timestamp,
                payload.Signature.Token,
                payload.Signature.Signature,
                signingKey))
        {
            throw new BadRequestException("Invalid HMAC signature");
        }
    }

    /// <summary>
    /// Resolves the workspace route for the recipient local part.
    /// Throws if no active route matches.
    /// </summary>
    private async Task<InboundEmailRoute> ResolveRouteAsync(InboundEmailPayload payload)
    {
        var localPart = payload.GetRecipientLocalPart();
        if (string.IsNullOrWhiteSpace(localPart))
        {
            throw new BadRequestException("Could not determine recipient local part");
        }

        var route = await this.dbContext.InboundEmailRoutes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.LocalPart == localPart && r.Active);

        if (route == null)
        {
            this.logger.LogWarning("No active route found for local part: {LocalPart}", localPart);
            throw new BadRequestException($"No active route for {localPart}@{this.config.InboundEmail.Domain}");
        }

        return route;
    }

    /// <summary>
    /// Creates the InboundEmail entity record from the payload.
    /// </summary>
    private async Task<InboundEmail> CreateInboundEmailRecordAsync(
        InboundEmailPayload payload,
        InboundEmailRoute route,
        CancellationToken ct)
    {
        var localPart = payload.GetRecipientLocalPart();

        var inboundEmail = new InboundEmail
        {
            WorkspaceId = route.WorkspaceId,
            RouteId = route.Id,
            FromEmail = payload.GetSenderEmail(),
            FromName = payload.FromName,
            ToEmail = $"{localPart}@{this.config.InboundEmail.Domain}",
            Subject = payload.Subject ?? "(no subject)",
            BodyPlain = payload.BodyPlain ?? payload.StrippedText ?? string.Empty,
            BodyHtml = payload.BodyHtml ?? payload.StrippedHtml,
            MessageId = payload.GetMessageId(),

            // InReplyToEmailId is reserved for Phase 2 (reply threading)
            InReplyToEmailId = null,

            Status = "Pending",
            RawPayload = System.Text.Json.JsonSerializer.Serialize(payload),
            ReceivedAt = DateTime.UtcNow,
        };

        this.dbContext.InboundEmails.Add(inboundEmail);
        await this.dbContext.SaveChangesAsync(ct);

        return inboundEmail;
    }

    /// <summary>
    /// Finds an existing contact by email, or creates one from the sender info.
    /// </summary>
    private async Task<Contact?> ResolveContactAsync(InboundEmail inboundEmail, CancellationToken ct)
    {
        var email = inboundEmail.FromEmail.Trim().ToLowerInvariant();

        var contact = await this.dbContext.Contacts
            .FirstOrDefaultAsync(c => c.Email.ToLower() == email
                && c.WorkspaceId == inboundEmail.WorkspaceId, ct);

        if (contact != null)
        {
            return contact;
        }

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

    /// <summary>
    /// Validates attachment size and MIME type, computes a content-hash storage path,
    /// and uploads each attachment to RustFS.
    /// </summary>
    private async Task<List<InboundEmailAttachment>> ProcessAttachmentsAsync(
        InboundEmail inboundEmail,
        int workspaceId,
        Dictionary<string, (Stream Stream, string ContentType, long Size)>? attachmentStreams,
        CancellationToken ct)
    {
        var stored = new List<InboundEmailAttachment>();

        if (attachmentStreams == null || attachmentStreams.Count == 0)
        {
            return stored;
        }

        var maxSize = this.config.InboundEmail.MaxAttachmentSize;
        var allowedTypes = this.config.InboundEmail.AllowedMimeTypes?
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

            // Compute content hash and build storage path
            var (storagePath, publicUrl) = await this.StoreAttachmentWithHashAsync(
                stream, inboundEmail.Id, fileName, contentType);

            if (storagePath != null)
            {
                attachment.StoragePath = storagePath;
                attachment.PublicUrl = publicUrl;
                attachment.IsStored = true;

                this.dbContext.InboundEmailAttachments.Add(attachment);
                await this.dbContext.SaveChangesAsync(ct);
                stored.Add(attachment);

                this.logger.LogInformation(
                    "Stored attachment {FileName} → {StoragePath}", fileName, storagePath);
            }
            else
            {
                attachment.IsStored = false;
                this.dbContext.InboundEmailAttachments.Add(attachment);
                await this.dbContext.SaveChangesAsync(ct);
                stored.Add(attachment);
            }
        }

        return stored;
    }

    /// <summary>
    /// Computes a SHA256 hash of the attachment content and stores it under
    /// inbound/{inboundEmailId}/{hash}{extension}.
    /// Returns (storagePath, publicUrl) on success, or null values on failure.
    /// </summary>
    private async Task<(string? storagePath, string? publicUrl)> StoreAttachmentWithHashAsync(
        Stream stream,
        int inboundEmailId,
        string fileName,
        string contentType)
    {
        // Compute SHA256 hash from the stream content
        byte[] hashBytes;
        var originalPosition = stream.Position;

        try
        {
            hashBytes = await SHA256.HashDataAsync(stream);
        }
        finally
        {
            stream.Position = originalPosition;
        }

        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        var extension = Path.GetExtension(fileName);
        var storagePath = $"inbound/{inboundEmailId}/{hash}{extension}";

        try
        {
            var publicUrl = await this.fileStorageService.UploadFileAsync(storagePath, stream, contentType);
            return (storagePath, publicUrl);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to store attachment {FileName} at {Path}", fileName, storagePath);
            return (null, null);
        }
    }

    /// <summary>
    /// Creates a ticket from the inbound email using the route's defaults.
    /// </summary>
    private async Task<Ticket> CreateTicketAsync(
        InboundEmailRoute route,
        InboundEmail inboundEmail,
        Contact? contact)
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

        return await this.ticketCreationService.CreateTicketAsync(
            route.WorkspaceId,
            request,
            SystemUserId);
    }

    /// <summary>
    /// Attempts to resolve this inbound email as a reply to an existing ticket.
    /// Strategy: (1) In-Reply-To header matching → (2) subject + contact matching.
    /// Returns the matched ticket, or null if no match.
    /// </summary>
    private async Task<Ticket?> TryResolveReplyTicketAsync(
        InboundEmailPayload payload,
        InboundEmailRoute route,
        Contact? contact,
        CancellationToken ct)
    {
        // Strategy 1: Match by In-Reply-To header → previous InboundEmail.MessageId
        if (!string.IsNullOrWhiteSpace(payload.InReplyTo))
        {
            var originalInbound = await this.dbContext.InboundEmails
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.MessageId == payload.InReplyTo, ct);

            if (originalInbound?.TicketId != null)
            {
                var ticket = await this.dbContext.Tickets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == originalInbound.TicketId
                        && t.WorkspaceId == route.WorkspaceId, ct);

                if (ticket != null)
                {
                    this.logger.LogInformation(
                        "Resolved reply via In-Reply-To: {InReplyTo} → ticket {TicketId}",
                        payload.InReplyTo, ticket.Id);
                    return ticket;
                }
            }
        }

        // Strategy 2: Subject-based matching (strip Re:/Fwd: prefixes)
        if (contact == null || string.IsNullOrWhiteSpace(payload.Subject))
        {
            return null;
        }

        var cleanSubject = StripReplyPrefix(payload.Subject);
        if (string.IsNullOrWhiteSpace(cleanSubject))
        {
            return null;
        }

        var contactEmail = contact.Email.Trim().ToLowerInvariant();

        var matchedTicket = await this.dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.WorkspaceId == route.WorkspaceId
                && t.ContactId == contact.Id
                && t.Subject.ToLower() == cleanSubject.ToLowerInvariant())
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (matchedTicket != null)
        {
            this.logger.LogInformation(
                "Resolved reply via subject match: '{Subject}' → ticket {TicketId}",
                cleanSubject, matchedTicket.Id);
        }

        return matchedTicket;
    }

    /// <summary>
    /// Handles an inbound email that was resolved as a reply to an existing ticket.
    /// Adds the email body as a client-visible comment and links attachments.
    /// </summary>
    private async Task HandleReplyAsync(
        InboundEmail inboundEmail,
        Ticket replyTicket,
        Contact? contact,
        List<InboundEmailAttachment> storedAttachments,
        CancellationToken ct)
    {
        // Build the comment body from the inbound email
        var commentBody = inboundEmail.BodyPlain ?? inboundEmail.BodyHtml ?? "(no content)";
        var prefix = inboundEmail.FromName != null
            ? $"**{inboundEmail.FromName}** <{inboundEmail.FromEmail}> replied via email:\n\n"
            : $"**{inboundEmail.FromEmail}** replied via email:\n\n";

        // Add comment and notify assignees (visible to client since it's from the contact)
        await this.ticketCommentService.AddCommentAndNotifyAsync(
            replyTicket.WorkspaceId,
            replyTicket.Id,
            SystemUserId,
            prefix + commentBody,
            isVisibleToClient: true,
            ct);

        // Link attachments to the reply ticket
        if (storedAttachments.Count > 0)
        {
            await this.LinkAttachmentsToTicketAsync(
                storedAttachments, replyTicket.Id, replyTicket.WorkspaceId, ct);
        }

        // Update inbound email record
        inboundEmail.TicketId = replyTicket.Id;
        inboundEmail.ContactId = contact?.Id;
        inboundEmail.InReplyToEmailId = await this.dbContext.InboundEmails
            .AsNoTracking()
            .Where(e => e.WorkspaceId == replyTicket.WorkspaceId
                && e.TicketId == replyTicket.Id)
            .OrderBy(e => e.Id)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);
        inboundEmail.Status = "Processed";
        inboundEmail.ProcessedAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Strips common reply/forward prefixes from a subject line.
    /// Returns the cleaned subject, or null if empty.
    /// </summary>
    public static string? StripReplyPrefix(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var trimmed = subject.Trim();

        // Strip leading "Re:", "RE:", "Fwd:", "FW:" (with optional whitespace and brackets)
        while (true)
        {
            var lower = trimmed.ToLowerInvariant();
            var changed = false;

            if (lower.StartsWith("re:", StringComparison.Ordinal))
            {
                trimmed = trimmed[3..].TrimStart();
                changed = true;
            }
            else if (lower.StartsWith("fwd:", StringComparison.Ordinal)
                || lower.StartsWith("fw:", StringComparison.Ordinal))
            {
                trimmed = trimmed[(lower.StartsWith("fwd:", StringComparison.Ordinal) ? 4 : 3)..].TrimStart();
                changed = true;
            }

            if (!changed)
            {
                break;
            }
        }

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Links successfully stored attachments to the created ticket via FileStorage records.
    /// </summary>
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

    /// <summary>
    /// Sends a confirmation email for the processed inbound ticket.
    /// Failures are logged but do not block the pipeline.
    /// </summary>
    private async Task SendConfirmationAsync(InboundEmail inboundEmail)
    {
        try
        {
            var variables = new Dictionary<string, string>
            {
                ["ticket_id"] = inboundEmail.TicketId?.ToString(CultureInfo.InvariantCulture) ?? "?",
                ["subject"] = inboundEmail.Subject,
                ["contact_name"] = inboundEmail.FromName ?? inboundEmail.FromEmail,
            };

            await this.emailSendService.AddToQueueAsync(
                inboundEmail.FromEmail,
                EmailTemplateType.TicketReceived,
                variables,
                sentByUserId: SystemUserId);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex,
                "Failed to queue confirmation email for inbound email {EmailId}: {Message}",
                inboundEmail.Id, ex.Message);
        }
    }
}
