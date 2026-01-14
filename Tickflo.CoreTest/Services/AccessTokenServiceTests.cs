using Tickflo.Core.Services;
using Xunit;

namespace Tickflo.CoreTest.Services;

/// <summary>
/// Tests for AccessTokenService.
/// </summary>
public class AccessTokenServiceTests
{
    [Fact]
    public void GenerateToken_DefaultLength_Returns32Characters()
    {
        // Arrange
        var service = new AccessTokenService();

        // Act
        var token = service.GenerateToken();

        // Assert
        Assert.Equal(32, token.Length);
    }

    [Fact]
    public void GenerateToken_CustomLength_ReturnsCorrectLength()
    {
        // Arrange
        var service = new AccessTokenService();
        var customLength = 64;

        // Act
        var token = service.GenerateToken(customLength);

        // Assert
        Assert.Equal(customLength, token.Length);
    }

    [Fact]
    public void GenerateToken_ReturnsUnique()
    {
        // Arrange
        var service = new AccessTokenService();

        // Act
        var token1 = service.GenerateToken();
        var token2 = service.GenerateToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_ContainsValidCharacters()
    {
        // Arrange
        var service = new AccessTokenService();
        var validCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        // Act
        var token = service.GenerateToken();

        // Assert
        Assert.True(token.All(c => validCharacters.Contains(c)), "Token contains invalid characters");
    }

    [Fact]
    public void GenerateToken_LargeLength_Returns()
    {
        // Arrange
        var service = new AccessTokenService();
        var largeLength = 256;

        // Act
        var token = service.GenerateToken(largeLength);

        // Assert
        Assert.Equal(largeLength, token.Length);
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_MinimalLength_Returns()
    {
        // Arrange
        var service = new AccessTokenService();

        // Act
        var token = service.GenerateToken(1);

        // Assert
        Assert.Equal(1, token.Length);
    }
}
