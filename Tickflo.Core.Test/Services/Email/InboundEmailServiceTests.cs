namespace Tickflo.CoreTest.Services.Email;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.DTOs;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Storage;
using Tickflo.Core.Services.Tickets;
using Xunit;

public class InboundEmailHMACValidatorTests
{
    private readonly InboundEmailHMACValidator validator = new();

    [Fact]
    public void Validate_WithCorrectSignature_ReturnsTrue()
    {
        // Arrange
        var signingKey = "test-signing-key-12345";
        var timestamp = "1728000000";
        var token = "random-test-token-abc";

        // Compute the expected HMAC the same way Mailgun would
        var data = timestamp + token;
        var hmacBytes = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(signingKey),
            System.Text.Encoding.UTF8.GetBytes(data));
        var expectedSignature = Convert.ToHexString(hmacBytes).ToLowerInvariant();

        // Act
        var result = this.validator.Validate(timestamp, token, expectedSignature, signingKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_WithWrongSignature_ReturnsFalse()
    {
        // Arrange
        var signingKey = "test-signing-key-12345";
        var timestamp = "1728000000";
        var token = "random-test-token-abc";

        // Act
        var result = this.validator.Validate(timestamp, token, "wrong-signature", signingKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithEmptySigningKey_ReturnsFalse()
    {
        // Act
        var result = this.validator.Validate("ts", "token", "sig", string.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithNullSigningKey_ReturnsFalse()
    {
        // Act
        var result = this.validator.Validate("ts", "token", "sig", null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Validate_WithEmptyTimestamp_ReturnsFalse()
    {
        var result = this.validator.Validate(string.Empty, "token", "sig", "key");
        Assert.False(result);
    }

    [Fact]
    public void Validate_TimingSafeEquality_SameLengthDifferentValues_ReturnsFalse()
    {
        var result = this.validator.Validate("ts", "token", "abcdef1234567890", "key");
        Assert.False(result);
    }
}

public class InboundEmailServiceTests
{
    private static TickfloDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TickfloDbContext(options);
    }

    private static InboundEmailConfig CreateTestConfig() => new()
    {
        Domain = "inbound.tickflo.co",
        WebhookSigningKey = "test-webhook-key",
        MaxAttachmentSize = 25 * 1024 * 1024,
        AllowedMimeTypes = "image/jpeg,image/png,application/pdf,text/plain,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    [Fact]
    public async Task ProcessAsync_WithMatchingRoute_CreatesTicket()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Set up workspace and route
        var workspace = new Workspace { Name = "Test Workspace", Slug = "test-ws" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute
        {
            WorkspaceId = workspace.Id,
            LocalPart = "support",
            Label = "Support Inbox",
            Active = true,
        };
        db.InboundEmailRoutes.Add(route);
        await db.SaveChangesAsync();

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "customer@example.com",
            Sender = "customer@example.com",
            FromName = "Jane Customer",
            Subject = "Need help with my account",
            BodyPlain = "I can't log in to the portal.",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-001@example.com>",
            Signature = new MailgunSignature
            {
                Timestamp = "1728000000",
                Token = "token-abc",
                Signature = "valid-sig",
            },
        };

        ticketCreationService.Setup(s => s.CreateTicketAsync(
                workspace.Id,
                It.IsAny<TicketCreationRequest>(),
                It.IsAny<int>()))
            .ReturnsAsync(new Ticket
            {
                Id = 100,
                Subject = payload.Subject,
                Description = payload.BodyPlain,
                WorkspaceId = workspace.Id,
            });

        // Act
        var result = await service.ProcessAsync(payload, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Processed", result.Status);
        Assert.Equal(100, result.TicketId);

        // Verify inbound email was stored
        var stored = await db.InboundEmails.FirstOrDefaultAsync();
        Assert.NotNull(stored);
        Assert.Equal("customer@example.com", stored.FromEmail);
        Assert.Equal("support@inbound.tickflo.co", stored.ToEmail);
        Assert.Equal("Processed", stored.Status);
        Assert.NotNull(stored.ProcessedAt);
        Assert.Equal(100, stored.TicketId);

        // Verify contact was created
        var contact = await db.Contacts.FirstOrDefaultAsync();
        Assert.NotNull(contact);
        Assert.Equal("Jane Customer", contact.Name);
        Assert.Equal("customer@example.com", contact.Email);

        // Verify confirmation email was sent
        emailSendService.Verify(s => s.AddToQueueAsync(
            "customer@example.com",
            EmailTemplateType.TicketReceived,
            It.Is<Dictionary<string, string>>(d =>
                d["ticket_id"] == "100" &&
                d["subject"] == "Need help with my account" &&
                d["contact_name"] == "Jane Customer"),
            1), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WithUnknownLocalPart_ThrowsBadRequest()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "spammer@evil.com",
            Sender = "spammer@evil.com",
            Subject = "Buy cheap stuff",
            BodyPlain = "Click here!",
            Recipient = "unknown@inbound.tickflo.co",
            MessageId = "<msg-spam@evil.com>",
            Signature = new MailgunSignature
            {
                Timestamp = "1728000000",
                Token = "token-xyz",
                Signature = "sig",
            },
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.ProcessAsync(payload, null));
        Assert.Contains("No active route", ex.Message);
        emailSendService.Verify(s => s.AddToQueueAsync(It.IsAny<string>(), It.IsAny<EmailTemplateType>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WithDuplicateMessageId_ReturnsDuplicate()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Already processed email
        db.InboundEmails.Add(new InboundEmail
        {
            WorkspaceId = 1,
            MessageId = "<msg-duplicate@example.com>",
            FromEmail = "prev@example.com",
            ToEmail = "test@inbound.tickflo.co",
            Subject = "Previous",
            BodyPlain = "Already processed",
            Status = "Processed",
            ReceivedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "customer@example.com",
            Sender = "customer@example.com",
            Subject = "Duplicate",
            BodyPlain = "This is a dup",
            Recipient = "test@inbound.tickflo.co",
            MessageId = "<msg-duplicate@example.com>",
            Signature = new MailgunSignature
            {
                Timestamp = "1728000000",
                Token = "token-dup",
                Signature = "sig",
            },
        };

        // Act
        var result = await service.ProcessAsync(payload, null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already processed", result.Message);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidHmac_ThrowsBadRequest()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        // HMAC validation fails
        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "hacker@evil.com",
            Sender = "hacker@evil.com",
            Subject = "Hack attempt",
            BodyPlain = "Malicious content",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-hack@evil.com>",
            Signature = new MailgunSignature
            {
                Timestamp = "1728000000",
                Token = "token-hack",
                Signature = "invalid-sig",
            },
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.ProcessAsync(payload, null));
        Assert.Contains("Invalid HMAC", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_WithFileAttachments_StoresFiles()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var workspace = new Workspace { Name = "Test WS", Slug = "test" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute
        {
            WorkspaceId = workspace.Id,
            LocalPart = "support",
            Label = "Support",
            Active = true,
        };
        db.InboundEmailRoutes.Add(route);
        await db.SaveChangesAsync();

        // Mock file upload
        fileStorageService.Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("https://storage.tickflo.co/invoice.pdf");

        ticketCreationService.Setup(s => s.CreateTicketAsync(
                workspace.Id, It.IsAny<TicketCreationRequest>(), It.IsAny<int>()))
            .ReturnsAsync(new Ticket { Id = 42, Subject = "With attachment", WorkspaceId = workspace.Id });

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "vendor@supplier.com",
            Sender = "vendor@supplier.com",
            Subject = "Invoice for March",
            BodyPlain = "Please find attached invoice.",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-attach@supplier.com>",
            Signature = new MailgunSignature
            {
                Timestamp = "1728000000",
                Token = "token-attach",
                Signature = "valid-sig",
            },
            AttachmentCount = 1,
        };

        var attachmentStreams = new Dictionary<string, (Stream, string, long)>
        {
            ["invoice.pdf"] = (new MemoryStream("fake-pdf-content"u8.ToArray()), "application/pdf", 100),
        };

        // Act
        var result = await service.ProcessAsync(payload, attachmentStreams);

        // Assert
        Assert.True(result.Success);

        // Verify attachment records were created
        var attachments = await db.InboundEmailAttachments.ToListAsync();
        Assert.NotEmpty(attachments);
        var attachment = Assert.Single(attachments);
        Assert.Equal("invoice.pdf", attachment.FileName);
        Assert.True(attachment.IsStored);

        // Verify file was uploaded to storage
        fileStorageService.Verify(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), "application/pdf"), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_AttachmentTooLarge_RejectsFile()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig
        {
            InboundEmail = new InboundEmailConfig
            {
                Domain = "inbound.tickflo.co",
                WebhookSigningKey = "test-key",
                MaxAttachmentSize = 100, // Only allow 100 bytes
            },
        };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var workspace = new Workspace { Name = "Test", Slug = "test" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute { WorkspaceId = workspace.Id, LocalPart = "support", Active = true };
        db.InboundEmailRoutes.Add(route);
        await db.SaveChangesAsync();

        ticketCreationService.Setup(s => s.CreateTicketAsync(
                workspace.Id, It.IsAny<TicketCreationRequest>(), It.IsAny<int>()))
            .ReturnsAsync(new Ticket { Id = 7, Subject = "Large file test", WorkspaceId = workspace.Id });

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "user@example.com",
            Sender = "user@example.com",
            Subject = "Test large file",
            BodyPlain = "Has large attachment",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-large@example.com>",
            Signature = new MailgunSignature { Timestamp = "1", Token = "t", Signature = "s" },
        };

        var attachmentStreams = new Dictionary<string, (Stream, string, long)>
        {
            ["big_file.zip"] = (new MemoryStream(new byte[200]), "application/zip", 200),
        };

        // Act
        var result = await service.ProcessAsync(payload, attachmentStreams);

        // Assert
        Assert.True(result.Success);
        var attachment = await db.InboundEmailAttachments.FirstOrDefaultAsync();
        Assert.NotNull(attachment);
        Assert.False(attachment.IsStored);

        // Verify file was NOT uploaded
        fileStorageService.Verify(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_ExistingContact_DoesNotCreateDuplicate()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var workspace = new Workspace { Name = "Test", Slug = "test" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute { WorkspaceId = workspace.Id, LocalPart = "support", Active = true };
        db.InboundEmailRoutes.Add(route);

        // Existing contact with same email
        var existingContact = new Contact
        {
            WorkspaceId = workspace.Id,
            Name = "Existing Contact",
            Email = "returning@example.com",
        };
        db.Contacts.Add(existingContact);
        await db.SaveChangesAsync();

        var contactId = existingContact.Id;

        ticketCreationService.Setup(s => s.CreateTicketAsync(
                workspace.Id, It.IsAny<TicketCreationRequest>(), It.IsAny<int>()))
            .ReturnsAsync(new Ticket { Id = 99, Subject = "Returning customer", WorkspaceId = workspace.Id });

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object);

        var payload = new InboundEmailPayload
        {
            From = "returning@example.com",
            Sender = "returning@example.com",
            Subject = "Back again",
            BodyPlain = "Need more help",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-return@example.com>",
            Signature = new MailgunSignature { Timestamp = "1", Token = "t", Signature = "s" },
        };

        // Act
        var result = await service.ProcessAsync(payload, null);

        // Assert
        Assert.True(result.Success);

        // Verify no new contact was created
        var contacts = await db.Contacts.Where(c => c.Email == "returning@example.com").ToListAsync();
        Assert.Single(contacts);
        Assert.Equal(contactId, contacts[0].Id);
    }

    // ──── Reply detection: StripReplyPrefix unit tests ────

    [Theory]
    [InlineData("Re: Need help", "Need help")]
    [InlineData("RE: Need help", "Need help")]
    [InlineData("re: Need help", "Need help")]
    [InlineData("Fwd: Invoice", "Invoice")]
    [InlineData("FW: Invoice", "Invoice")]
    [InlineData("Re: Fwd: Original subject", "Original subject")]
    [InlineData("   Re: Extra spaces   ", "Extra spaces")]
    [InlineData("No prefix here", "No prefix here")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void StripReplyPrefix_ReturnsExpected(string input, string? expected)
    {
        var result = InboundEmailService.StripReplyPrefix(input);
        Assert.Equal(expected, result);
    }

    // ──── Reply detection: In-Reply-To header matching ────

    [Fact]
    public async Task ProcessAsync_WithInReplyToHeader_AddsCommentToExistingTicket()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var workspace = new Workspace { Name = "Test WS", Slug = "test" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute
        {
            WorkspaceId = workspace.Id,
            LocalPart = "support",
            Label = "Support",
            Active = true,
        };
        db.InboundEmailRoutes.Add(route);

        var contact = new Contact
        {
            WorkspaceId = workspace.Id,
            Name = "Jane",
            Email = "jane@example.com",
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var ticket = new Ticket
        {
            WorkspaceId = workspace.Id,
            Subject = "Need help with login",
            ContactId = contact.Id,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Add an existing InboundEmail record (simulating a previously processed inbound email)
        var previousInbound = new InboundEmail
        {
            WorkspaceId = workspace.Id,
            RouteId = route.Id,
            FromEmail = "jane@example.com",
            Subject = "Need help with login",
            MessageId = "<msg-original@example.com>",
            TicketId = ticket.Id,
            Status = "Processed",
        };
        db.InboundEmails.Add(previousInbound);
        await db.SaveChangesAsync();

        var payload = new InboundEmailPayload
        {
            From = "jane@example.com",
            Sender = "jane@example.com",
            FromName = "Jane",
            Subject = "Re: Need help with login",
            BodyPlain = "I tried resetting but still can't log in.",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-reply@example.com>",
            InReplyTo = "<msg-original@example.com>", // Matches previous inbound
            Signature = new MailgunSignature { Timestamp = "1", Token = "t", Signature = "s" },
        };

        // Act
        var result = await new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object)
            .ProcessAsync(payload, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ticket.Id, result.TicketId);

        // Verify no new ticket was created
        var ticketCount = await db.Tickets.CountAsync();
        Assert.Equal(1, ticketCount);

        // Verify comment was added
        ticketCommentService.Verify(
            c => c.AddCommentAndNotifyAsync(
                workspace.Id, ticket.Id, It.IsAny<int>(),
                It.Is<string>(s => s.Contains("I tried resetting")),
                true, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify ticket creation was NOT called
        ticketCreationService.Verify(
            c => c.CreateTicketAsync(It.IsAny<int>(), It.IsAny<TicketCreationRequest>(), It.IsAny<int>()),
            Times.Never);
    }

    // ──── Reply detection: Subject-based matching (no In-Reply-To) ────

    [Fact]
    public async Task ProcessAsync_WithReSubject_MatchesTicketByContactAndSubject()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var workspace = new Workspace { Name = "Test WS", Slug = "test" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute
        {
            WorkspaceId = workspace.Id,
            LocalPart = "support",
            Label = "Support",
            Active = true,
        };
        db.InboundEmailRoutes.Add(route);

        var contact = new Contact
        {
            WorkspaceId = workspace.Id,
            Name = "Bob",
            Email = "bob@example.com",
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        var ticket = new Ticket
        {
            WorkspaceId = workspace.Id,
            Subject = "Billing question",
            ContactId = contact.Id,
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var payload = new InboundEmailPayload
        {
            From = "bob@example.com",
            Sender = "bob@example.com",
            FromName = "Bob",
            Subject = "Re: Billing question", // Matches ticket subject (stripped)
            BodyPlain = "Can you send me the invoice for last month?",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-no-inreplyto@example.com>",
            InReplyTo = null, // No In-Reply-To header — falls back to subject matching
            Signature = new MailgunSignature { Timestamp = "1", Token = "t", Signature = "s" },
        };

        // Act
        var result = await new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object)
            .ProcessAsync(payload, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ticket.Id, result.TicketId);
        Assert.Equal(1, await db.Tickets.CountAsync());

        ticketCommentService.Verify(
            c => c.AddCommentAndNotifyAsync(
                workspace.Id, ticket.Id, It.IsAny<int>(),
                It.Is<string>(s => s.Contains("invoice for last month")),
                true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ──── Reply detection: No match → still creates ticket ────

    [Fact]
    public async Task ProcessAsync_WithNoReplyMatch_CreatesNewTicket()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var ticketCommentService = new Mock<ITicketCommentService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var workspace = new Workspace { Name = "Test WS", Slug = "test" };
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync();

        var route = new InboundEmailRoute
        {
            WorkspaceId = workspace.Id,
            LocalPart = "support",
            Label = "Support",
            Active = true,
        };
        db.InboundEmailRoutes.Add(route);
        await db.SaveChangesAsync();

        var createdTicket = new Ticket { WorkspaceId = workspace.Id, Subject = "New Issue" };
        ticketCreationService
            .Setup(s => s.CreateTicketAsync(workspace.Id, It.IsAny<TicketCreationRequest>(), It.IsAny<int>()))
            .ReturnsAsync(createdTicket);

        var payload = new InboundEmailPayload
        {
            From = "new@example.com",
            Sender = "new@example.com",
            Subject = "Brand new issue",
            BodyPlain = "This is a new request.",
            Recipient = "support@inbound.tickflo.co",
            MessageId = "<msg-new@example.com>",
            InReplyTo = null, // No In-Reply-To
            Signature = new MailgunSignature { Timestamp = "1", Token = "t", Signature = "s" },
        };

        // Act
        var result = await new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, ticketCommentService.Object, fileStorageService.Object, logger.Object)
            .ProcessAsync(payload, null);

        // Assert
        Assert.True(result.Success);

        // Verify ticket WAS created (not matched as reply)
        ticketCreationService.Verify(
            c => c.CreateTicketAsync(workspace.Id, It.IsAny<TicketCreationRequest>(), It.IsAny<int>()),
            Times.Once);

        // Verify no comment was added (not a reply)
        ticketCommentService.Verify(
            c => c.AddCommentAndNotifyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
