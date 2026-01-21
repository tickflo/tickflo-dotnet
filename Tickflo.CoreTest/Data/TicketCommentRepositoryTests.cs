namespace Tickflo.CoreTest.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TicketCommentRepositoryTests
{
    private static TickfloDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TickfloDbContext(options);
    }

    [Fact]
    public async Task ListByTicketAsyncReturnsAllCommentsForTicket()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var comments = new List<TicketComment>
        {
            new() {
                Id = 1,
                WorkspaceId = 1,
                TicketId = 10,
                CreatedByUserId = 1,
                Content = "First comment",
                IsVisibleToClient = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-2)
            },
            new() {
                Id = 2,
                WorkspaceId = 1,
                TicketId = 10,
                CreatedByUserId = 1,
                Content = "Second comment",
                IsVisibleToClient = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new() {
                Id = 3,
                WorkspaceId = 1,
                TicketId = 11,
                CreatedByUserId = 1,
                Content = "Different ticket",
                IsVisibleToClient = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        context.TicketComments.AddRange(comments);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.ListByTicketAsync(1, 10);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal(10, c.TicketId));
        Assert.Equal("First comment", result[0].Content);
        Assert.Equal("Second comment", result[1].Content);
    }

    [Fact]
    public async Task ListByTicketAsyncReturnsEmptyWhenNoComments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.ListByTicketAsync(1, 999);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindAsyncReturnsCommentById()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var comment = new TicketComment
        {
            Id = 1,
            WorkspaceId = 1,
            TicketId = 10,
            CreatedByUserId = 1,
            Content = "Test comment",
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TicketComments.Add(comment);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.FindAsync(1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test comment", result.Content);
        Assert.NotNull(result.CreatedByUser);
    }

    [Fact]
    public async Task FindAsyncReturnsNullWhenCommentNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.FindAsync(1, 999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsyncReturnsNullWhenCommentInDifferentWorkspace()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var comment = new TicketComment
        {
            Id = 1,
            WorkspaceId = 1,
            TicketId = 10,
            CreatedByUserId = 1,
            Content = "Test comment",
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TicketComments.Add(comment);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.FindAsync(2, 1); // Different workspace

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsyncAddsCommentAndReturnsWithId()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var comment = new TicketComment
        {
            WorkspaceId = 1,
            TicketId = 10,
            CreatedByUserId = 1,
            Content = "New comment",
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };

        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.CreateAsync(comment);

        // Assert
        Assert.NotEqual(0, result.Id);
        var retrieved = await context.TicketComments.FindAsync(result.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("New comment", retrieved.Content);
    }

    [Fact]
    public async Task UpdateAsyncModifiesComment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var comment = new TicketComment
        {
            Id = 1,
            WorkspaceId = 1,
            TicketId = 10,
            CreatedByUserId = 1,
            Content = "Original content",
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TicketComments.Add(comment);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        comment.Content = "Updated content";
        comment.UpdatedAt = DateTime.UtcNow;
        comment.UpdatedByUserId = 2;
        var result = await repository.UpdateAsync(comment);

        // Assert
        Assert.Equal("Updated content", result.Content);
        Assert.Equal(2, result.UpdatedByUserId);

        var retrieved = await context.TicketComments.FindAsync(1);
        Assert.Equal("Updated content", retrieved!.Content);
    }

    [Fact]
    public async Task DeleteAsyncRemovesComment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var comment = new TicketComment
        {
            Id = 1,
            WorkspaceId = 1,
            TicketId = 10,
            CreatedByUserId = 1,
            Content = "Comment to delete",
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TicketComments.Add(comment);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        await repository.DeleteAsync(1, 1);

        // Assert
        var retrieved = await context.TicketComments.FindAsync(1);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsyncDoesNothingWhenCommentNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TicketCommentRepository(context);

        // Act & Assert (should not throw)
        await repository.DeleteAsync(1, 999);
    }

    [Fact]
    public async Task DeleteAsyncDoesNotDeleteCommentsFromDifferentWorkspace()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var comment = new TicketComment
        {
            Id = 1,
            WorkspaceId = 1,
            TicketId = 10,
            CreatedByUserId = 1,
            Content = "Comment",
            IsVisibleToClient = true,
            CreatedAt = DateTime.UtcNow
        };
        context.TicketComments.Add(comment);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        await repository.DeleteAsync(2, 1); // Different workspace

        // Assert
        var retrieved = await context.TicketComments.FindAsync(1);
        Assert.NotNull(retrieved); // Comment should still exist
    }

    [Fact]
    public async Task ListByTicketAsyncOrdersCommentsByCreatedAt()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var user = new User { Id = 1, Name = "Test User", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);

        var now = DateTime.UtcNow;
        var comments = new List<TicketComment>
        {
            new() { WorkspaceId = 1, TicketId = 10, CreatedByUserId = 1, Content = "Third", IsVisibleToClient = true, CreatedAt = now.AddMinutes(2) },
            new() { WorkspaceId = 1, TicketId = 10, CreatedByUserId = 1, Content = "First", IsVisibleToClient = true, CreatedAt = now },
            new() { WorkspaceId = 1, TicketId = 10, CreatedByUserId = 1, Content = "Second", IsVisibleToClient = true, CreatedAt = now.AddMinutes(1) }
        };
        context.TicketComments.AddRange(comments);
        await context.SaveChangesAsync();

        var repository = new TicketCommentRepository(context);

        // Act
        var result = await repository.ListByTicketAsync(1, 10);

        // Assert
        Assert.Equal("First", result[0].Content);
        Assert.Equal("Second", result[1].Content);
        Assert.Equal("Third", result[2].Content);
    }
}
