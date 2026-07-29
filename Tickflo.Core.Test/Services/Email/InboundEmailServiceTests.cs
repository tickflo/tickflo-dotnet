namespace Tickflo.CoreTest.Services.Email;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.DTOs;
using Tickflo.Core.Entities;
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

    private static InboundEmailConfig CreateTestConfig()
    {
        return new InboundEmailConfig
        {
            Domain = "inbound.tickflo.co",
            WebhookSigningKey = "test-webhook-key",
            MaxAttachmentSize = 25 * 1024 * 1024,
            AllowedMimeTypes = "image/jpeg,image/png,application/pdf,text/plain,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        };
    }

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
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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
    public async Task ProcessAsync_WithUnknownLocalPart_ReturnsRejected()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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

        // Act
        var result = await service.ProcessAsync(payload, null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No active route", result.Message);
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
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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
    public async Task ProcessAsync_WithInvalidHmac_ReturnsFailed()
    {
        // Arrange
        var db = CreateDbContext();
        var config = new TickfloConfig { InboundEmail = CreateTestConfig() };
        var hmacValidator = new Mock<IInboundEmailHMACValidator>();
        var emailSendService = new Mock<IEmailSendService>();
        var ticketCreationService = new Mock<ITicketCreationService>();
        var fileStorageService = new Mock<IFileStorageService>();
        var logger = new Mock<ILogger<InboundEmailService>>();

        // HMAC validation fails
        hmacValidator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var service = new InboundEmailService(
            db, config, hmacValidator.Object, emailSendService.Object,
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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

        // Act
        var result = await service.ProcessAsync(payload, null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid HMAC", result.Message);
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
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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
            ticketCreationService.Object, fileStorageService.Object, logger.Object);

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
}
