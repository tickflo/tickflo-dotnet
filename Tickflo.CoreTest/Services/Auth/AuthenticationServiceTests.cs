namespace Tickflo.CoreTest.Services.Auth;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsyncValidCredentialsReturnsSuccess()
    {
        var email = "user@example.com";
        var password = "securePassword";
        var passwordHash = "hashed";
        var userId = 1;
        var tokenValue = "generated-token";

        var user = new User { Id = userId, Email = email, PasswordHash = passwordHash };

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(repo => repo.FindByEmailAsync(email)).ReturnsAsync(user);

        var passwordHasherMock = new Mock<IPasswordHasher>();
        passwordHasherMock.Setup(hasher => hasher.Verify(email + password, passwordHash)).Returns(true);

        var tokenRepoMock = new Mock<ITokenRepository>();
        tokenRepoMock.Setup(repo => repo.CreateForUserIdAsync(userId))
                     .ReturnsAsync(new Token { Value = tokenValue });

        var service = new AuthenticationService(
            userRepoMock.Object,
            passwordHasherMock.Object,
            tokenRepoMock.Object
        );

        var result = await service.AuthenticateAsync(email, password);

        Assert.True(result.Success);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(tokenValue, result.Token);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsyncUserNotFoundReturnsError()
    {
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(repo => repo.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var service = new AuthenticationService(
            userRepoMock.Object,
            new Mock<IPasswordHasher>().Object,
            new Mock<ITokenRepository>().Object
        );

        var result = await service.AuthenticateAsync("missing@example.com", "pw");

        Assert.False(result.Success);
        Assert.Equal("Invalid username or password, please try again", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsyncInvalidPasswordReturnsError()
    {
        var email = "user@example.com";
        var user = new User { Id = 1, Email = email, PasswordHash = "hash" };

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.FindByEmailAsync(email)).ReturnsAsync(user);

        var passwordHasherMock = new Mock<IPasswordHasher>();
        passwordHasherMock.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var service = new AuthenticationService(
            userRepoMock.Object,
            passwordHasherMock.Object,
            new Mock<ITokenRepository>().Object
        );

        var result = await service.AuthenticateAsync(email, "wrong-password");

        Assert.False(result.Success);
        Assert.Equal("Invalid username or password, please try again", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateAsyncNullHashReturnsError()
    {
        var email = "user@example.com";
        var user = new User { Id = 1, Email = email, PasswordHash = null };

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(r => r.FindByEmailAsync(email)).ReturnsAsync(user);

        var passwordHasherMock = new Mock<IPasswordHasher>();

        var service = new AuthenticationService(
            userRepoMock.Object,
            passwordHasherMock.Object,
            new Mock<ITokenRepository>().Object
        );

        var result = await service.AuthenticateAsync(email, "wrong-password");

        Assert.False(result.Success);
        Assert.Equal("Invalid username or password, please try again", result.ErrorMessage);
    }
}

