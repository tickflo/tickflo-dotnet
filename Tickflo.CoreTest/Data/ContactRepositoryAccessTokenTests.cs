using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Data;

/// <summary>
/// Tests for ContactRepository access token functionality.
/// </summary>
public class ContactRepositoryAccessTokenTests
{
    private ContactRepository CreateRepository(out TickfloDbContext context)
    {
        var options = new DbContextOptionsBuilder<TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new TickfloDbContext(options);
        return new ContactRepository(context);
    }

    [Fact]
    public async Task FindByAccessTokenAsync_ReturnsContact_WhenTokenExists()
    {
        // Arrange
        var repo = CreateRepository(out var context);
        var contact = new Contact
        {
            Id = 1,
            WorkspaceId = 1,
            Name = "Test Contact",
            Email = "test@example.com",
            AccessToken = "unique_token_123"
        };
        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindByAccessTokenAsync("unique_token_123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Contact", result.Name);
    }

    [Fact]
    public async Task FindByAccessTokenAsync_ReturnsNull_WhenTokenDoesNotExist()
    {
        // Arrange
        var repo = CreateRepository(out var context);

        // Act
        var result = await repo.FindByAccessTokenAsync("nonexistent_token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByAccessTokenAsync_ReturnsNull_WhenTokenIsNull()
    {
        // Arrange
        var repo = CreateRepository(out var context);
        var contact = new Contact
        {
            Id = 1,
            WorkspaceId = 1,
            Name = "Test Contact",
            Email = "test@example.com",
            AccessToken = null
        };
        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindByAccessTokenAsync("any_token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByAccessTokenAsync_IsCaseSensitive()
    {
        // Arrange
        var repo = CreateRepository(out var context);
        var contact = new Contact
        {
            Id = 1,
            WorkspaceId = 1,
            Name = "Test Contact",
            Email = "test@example.com",
            AccessToken = "TestToken123"
        };
        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        // Act
        var resultCorrectCase = await repo.FindByAccessTokenAsync("TestToken123");
        var resultWrongCase = await repo.FindByAccessTokenAsync("testtoken123");

        // Assert
        Assert.NotNull(resultCorrectCase);
        Assert.Null(resultWrongCase);
    }

    [Fact]
    public async Task FindByAccessTokenAsync_ReturnsSingleContact_WhenMultipleContactsExist()
    {
        // Arrange
        var repo = CreateRepository(out var context);
        var contact1 = new Contact
        {
            Id = 1,
            WorkspaceId = 1,
            Name = "Contact 1",
            Email = "contact1@example.com",
            AccessToken = "token1"
        };
        var contact2 = new Contact
        {
            Id = 2,
            WorkspaceId = 1,
            Name = "Contact 2",
            Email = "contact2@example.com",
            AccessToken = "token2"
        };
        context.Contacts.Add(contact1);
        context.Contacts.Add(contact2);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.FindByAccessTokenAsync("token1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Contact 1", result.Name);
    }
}
