using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

/// <summary>
/// Tests for client comment functionality in the ticket comment system.
/// Verifies that client comments are properly created, tracked, and filtered.
/// </summary>
public class TicketCommentClientTests
{
    [Fact]
    public async Task AddClientCommentAsync_CreatesCommentWithContactTracking()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        TicketComment? capturedComment = null;
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()))
            .Callback<TicketComment, CancellationToken>((c, _) => capturedComment = c)
            .ReturnsAsync((TicketComment c, CancellationToken _) => 
            {
                c.Id = 1;
                return c;
            });

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.AddClientCommentAsync(
            workspaceId: 1,
            ticketId: 10,
            contactId: 5,
            content: "Client's comment"
        );

        // Assert
        Assert.NotNull(capturedComment);
        Assert.Equal(1, capturedComment.WorkspaceId);
        Assert.Equal(10, capturedComment.TicketId);
        Assert.Equal(1, capturedComment.CreatedByUserId); // System user
        Assert.Equal(5, capturedComment.CreatedByContactId); // Actual client
        Assert.Equal("Client's comment", capturedComment.Content);
        Assert.True(capturedComment.IsVisibleToClient); // Always visible to client
    }

    [Fact]
    public async Task AddClientCommentAsync_AlwaysMarksVisibleToClient()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        TicketComment? capturedComment = null;
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()))
            .Callback<TicketComment, CancellationToken>((c, _) => capturedComment = c)
            .ReturnsAsync((TicketComment c, CancellationToken _) => 
            {
                c.Id = 1;
                return c;
            });

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        await service.AddClientCommentAsync(1, 10, 5, "Test comment");

        // Assert
        Assert.NotNull(capturedComment);
        Assert.True(capturedComment.IsVisibleToClient, 
            "Client comments must always be visible to the client");
    }

    [Fact]
    public async Task AddClientCommentAsync_TrimsWhitespace()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        TicketComment? capturedComment = null;
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()))
            .Callback<TicketComment, CancellationToken>((c, _) => capturedComment = c)
            .ReturnsAsync((TicketComment c, CancellationToken _) => 
            {
                c.Id = 1;
                return c;
            });

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        await service.AddClientCommentAsync(1, 10, 5, "  Comment with spaces  ");

        // Assert
        Assert.NotNull(capturedComment);
        Assert.Equal("Comment with spaces", capturedComment.Content);
    }

    [Fact]
    public async Task AddClientCommentAsync_ThrowsOnInvalidContactId()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var service = new TicketCommentService(mockRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.AddClientCommentAsync(1, 10, 0, "Comment")); // Invalid contact ID
    }

    [Fact]
    public async Task AddClientCommentAsync_ThrowsOnEmptyContent()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var service = new TicketCommentService(mockRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            service.AddClientCommentAsync(1, 10, 5, "  ")); // Whitespace only
    }

    [Fact]
    public async Task GetCommentsAsync_FiltersClientCommentsCorrectly()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var staffComment = new TicketComment 
        { 
            Id = 1, 
            TicketId = 10, 
            CreatedByUserId = 3,
            CreatedByContactId = null,
            Content = "Staff comment", 
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        var clientComment = new TicketComment 
        { 
            Id = 2, 
            TicketId = 10, 
            CreatedByUserId = 1, // System user
            CreatedByContactId = 5, // Actual client
            Content = "Client comment", 
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow.AddHours(1)
        };

        var allComments = new List<TicketComment> { staffComment, clientComment };
        mockRepo.Setup(r => r.ListByTicketAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allComments);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.GetCommentsAsync(1, 10, isClientView: true);

        // Assert
        Assert.Equal(2, result.Count); // Both visible to client
        Assert.Contains(result, c => c.CreatedByContactId == 5); // Client comment included
        Assert.Contains(result, c => c.CreatedByContactId == null); // Staff comment included
    }

    [Fact]
    public async Task GetCommentsAsync_FiltersInternalComments()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var visibleComment = new TicketComment 
        { 
            Id = 1, 
            TicketId = 10, 
            Content = "Visible", 
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        var internalComment = new TicketComment 
        { 
            Id = 2, 
            TicketId = 10, 
            Content = "Internal", 
            IsVisibleToClient = false,
            CreatedAt = DateTime.UtcNow.AddHours(1)
        };

        var allComments = new List<TicketComment> { visibleComment, internalComment };
        mockRepo.Setup(r => r.ListByTicketAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allComments);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.GetCommentsAsync(1, 10, isClientView: true);

        // Assert
        Assert.Single(result);
        Assert.Equal("Visible", result[0].Content);
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsAllCommentsForInternalView()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var visibleComment = new TicketComment 
        { 
            Id = 1, 
            TicketId = 10, 
            Content = "Visible", 
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        var internalComment = new TicketComment 
        { 
            Id = 2, 
            TicketId = 10, 
            Content = "Internal", 
            IsVisibleToClient = false,
            CreatedAt = DateTime.UtcNow.AddHours(1)
        };

        var allComments = new List<TicketComment> { visibleComment, internalComment };
        mockRepo.Setup(r => r.ListByTicketAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allComments);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.GetCommentsAsync(1, 10, isClientView: false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Content == "Visible");
        Assert.Contains(result, c => c.Content == "Internal");
    }

    [Fact]
    public async Task AddClientCommentAsync_SetsCorrectTimestamp()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var beforeCall = DateTime.UtcNow;
        TicketComment? capturedComment = null;
        
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()))
            .Callback<TicketComment, CancellationToken>((c, _) => capturedComment = c)
            .ReturnsAsync((TicketComment c, CancellationToken _) => 
            {
                c.Id = 1;
                return c;
            });

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        await service.AddClientCommentAsync(1, 10, 5, "Test");
        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.NotNull(capturedComment);
        Assert.True(capturedComment.CreatedAt >= beforeCall && capturedComment.CreatedAt <= afterCall,
            "Comment timestamp should be set to current UTC time");
    }
}
