using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Data;

public class TokenRepositoryTests
{
    private TokenRepository CreateRepository(out TickfloDbContext context)
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new TickfloDbContext(options);
        var config = new TickfloConfig { SESSION_TIMEOUT_MINUTES = 30 };

        return new TokenRepository(context, config);
    }

    [Fact]
    public async Task CreateForUserIdAsync_CreatesTokenCorrectly()
    {
        // Arrange
        var repo = CreateRepository(out var db);

        // Act
        var token = await repo.CreateForUserIdAsync(42);

        // Assert
        Assert.Equal(42, token.UserId);
        Assert.False(string.IsNullOrWhiteSpace(token.Value));
        Assert.True(token.MaxAge > 0);
        Assert.True((DateTime.UtcNow - token.CreatedAt).TotalSeconds < 2);
    }

    [Fact]
    public async Task FindByUserIdAsync_ReturnsValidToken()
    {
        // Arrange
        var repo = CreateRepository(out var db);
        var token = new Token
        {
            UserId = 42,
            Value = "abc123",
            CreatedAt = DateTime.UtcNow,
            MaxAge = 1800
        };
        db.Tokens.Add(token);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.FindByUserIdAsync(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("abc123", result!.Value);
    }

    [Fact]
    public async Task FindByUserIdAsync_ReturnsNull_WhenTokenExpired()
    {
        // Arrange
        var repo = CreateRepository(out var db);
        var expiredToken = new Token
        {
            UserId = 99,
            Value = "expired",
            CreatedAt = DateTime.UtcNow.AddMinutes(-31), // expired
            MaxAge = 1800
        };
        db.Tokens.Add(expiredToken);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.FindByUserIdAsync(99);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByUserIdAsync_ReturnsNewestValidToken_WhenMultipleExist()
    {
        // Arrange
        var repo = CreateRepository(out var db);
        var now = DateTime.UtcNow;

        var olderToken = new Token
        {
            UserId = 123,
            Value = "old_token",
            CreatedAt = now.AddMinutes(-5),
            MaxAge = 1800 // Still valid
        };

        var newerToken = new Token
        {
            UserId = 123,
            Value = "new_token",
            CreatedAt = now,
            MaxAge = 1800
        };

        db.Tokens.AddRange(olderToken, newerToken);
        await db.SaveChangesAsync();

        // Act
        var result = await repo.FindByUserIdAsync(123);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_token", result!.Value);
    }
}
