namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TicketCommentServiceTests
{
    [Fact]
    public async Task GetCommentsAsyncReturnsAllCommentsForInternalView()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var comments = new List<TicketComment>
        {
            new() {
                Id = 1,
                TicketId = 10,
                Content = "Public comment",
                IsVisibleToClient = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = 2,
                TicketId = 10,
                Content = "Internal comment",
                IsVisibleToClient = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        mockRepo.Setup(r => r.ListByTicketAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.GetCommentsAsync(1, 10, isClientView: false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Content == "Public comment");
        Assert.Contains(result, c => c.Content == "Internal comment");
    }

    [Fact]
    public async Task GetCommentsAsyncFiltersInternalCommentsForClientView()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var comments = new List<TicketComment>
        {
            new() {
                Id = 1,
                TicketId = 10,
                Content = "Public comment",
                IsVisibleToClient = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = 2,
                TicketId = 10,
                Content = "Internal comment",
                IsVisibleToClient = false,
                CreatedAt = DateTime.UtcNow
            }
        };
        mockRepo.Setup(r => r.ListByTicketAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.GetCommentsAsync(1, 10, isClientView: true);

        // Assert
        Assert.Single(result);
        Assert.Equal("Public comment", result[0].Content);
        Assert.True(result[0].IsVisibleToClient);
    }

    [Fact]
    public async Task AddCommentAsyncCreatesCommentWithCorrectProperties()
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
        var result = await service.AddCommentAsync(
            workspaceId: 1,
            ticketId: 10,
            createdByUserId: 5,
            content: "Test comment",
            isVisibleToClient: true
        );

        // Assert
        Assert.NotNull(capturedComment);
        Assert.Equal(1, capturedComment.WorkspaceId);
        Assert.Equal(10, capturedComment.TicketId);
        Assert.Equal(5, capturedComment.CreatedByUserId);
        Assert.Equal("Test comment", capturedComment.Content);
        Assert.True(capturedComment.IsVisibleToClient);
        Assert.Equal(1, result.Id);
        mockRepo.Verify(r => r.CreateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsyncCreatesInternalCommentWhenNotVisibleToClient()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        TicketComment? capturedComment = null;
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()))
            .Callback<TicketComment, CancellationToken>((c, _) => capturedComment = c)
            .ReturnsAsync((TicketComment c, CancellationToken _) =>
            {
                c.Id = 2;
                return c;
            });

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        await service.AddCommentAsync(1, 10, 5, "Internal note", isVisibleToClient: false);

        // Assert
        Assert.NotNull(capturedComment);
        Assert.False(capturedComment.IsVisibleToClient);
        Assert.Equal("Internal note", capturedComment.Content);
    }

    [Fact]
    public async Task UpdateCommentAsyncUpdatesCommentContent()
    {
        // Arrange
        var existingComment = new TicketComment
        {
            Id = 1,
            TicketId = 10,
            WorkspaceId = 1,
            Content = "Old content",
            CreatedByUserId = 5,
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };

        var mockRepo = new Mock<ITicketCommentRepository>();
        mockRepo.Setup(r => r.FindAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingComment);
        TicketComment? updatedComment = null;
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()))
            .Callback<TicketComment, CancellationToken>((c, _) => updatedComment = c)
            .ReturnsAsync((TicketComment c, CancellationToken _) => c);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.UpdateCommentAsync(1, 1, "New content", 6);

        // Assert
        Assert.NotNull(updatedComment);
        Assert.Equal("New content", updatedComment.Content);
        Assert.Equal(6, updatedComment.UpdatedByUserId);
        Assert.NotNull(updatedComment.UpdatedAt);
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<TicketComment>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCommentAsyncThrowsWhenCommentNotFound()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        mockRepo.Setup(r => r.FindAsync(1, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketComment?)null);

        var service = new TicketCommentService(mockRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateCommentAsync(1, 999, "New content", 6));
    }

    [Fact]
    public async Task DeleteCommentAsyncCallsRepositoryDelete()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        var service = new TicketCommentService(mockRepo.Object);

        // Act
        await service.DeleteCommentAsync(1, 5);

        // Assert
        mockRepo.Verify(r => r.DeleteAsync(1, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCommentsAsyncReturnsEmptyListWhenNoComments()
    {
        // Arrange
        var mockRepo = new Mock<ITicketCommentRepository>();
        mockRepo.Setup(r => r.ListByTicketAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new TicketCommentService(mockRepo.Object);

        // Act
        var result = await service.GetCommentsAsync(1, 10);

        // Assert
        Assert.Empty(result);
    }
}
