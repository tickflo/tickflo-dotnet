using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;
using Xunit;

namespace Tickflo.CoreTest.Services.Auth;

public class PasswordSetupServiceTests
{
    [Fact]
    public async Task ValidateResetTokenAsync_MissingToken_ReturnsError()
    {
        var service = CreateService();

        var result = await service.ValidateResetTokenAsync(string.Empty);

        Assert.False(result.IsValid);
        Assert.Equal("Missing token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateResetTokenAsync_InvalidToken_ReturnsError()
    {
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("bad")).ReturnsAsync((Token?)null);
        var service = CreateService(tokenRepository: tokenRepo.Object);

        var result = await service.ValidateResetTokenAsync("bad");

        Assert.False(result.IsValid);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateResetTokenAsync_UserMissing_ReturnsError()
    {
        var token = new Token { UserId = 7, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(7)).ReturnsAsync((User?)null);

        var service = CreateService(userRepository: userRepo.Object, tokenRepository: tokenRepo.Object);

        var result = await service.ValidateResetTokenAsync("tok");

        Assert.False(result.IsValid);
        Assert.Equal("User not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateResetTokenAsync_Valid_ReturnsUser()
    {
        var token = new Token { UserId = 3, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var user = new User { Id = 3, Email = "u@example.com" };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(3)).ReturnsAsync(user);

        var service = CreateService(userRepository: userRepo.Object, tokenRepository: tokenRepo.Object);

        var result = await service.ValidateResetTokenAsync("tok");

        Assert.True(result.IsValid);
        Assert.Equal(3, result.UserId);
        Assert.Equal("u@example.com", result.UserEmail);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_InvalidToken_ReturnsError()
    {
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync((Token?)null);

        var service = CreateService(tokenRepository: tokenRepo.Object);

        var result = await service.SetPasswordWithTokenAsync("tok", "newpass123");

        Assert.False(result.Success);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_TooShort_ReturnsError()
    {
        var token = new Token { UserId = 5, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var user = new User { Id = 5, Email = "short@example.com" };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(user);

        var service = CreateService(userRepository: userRepo.Object, tokenRepository: tokenRepo.Object);

        var result = await service.SetPasswordWithTokenAsync("tok", "short");

        Assert.False(result.Success);
        Assert.Equal("Password must be at least 8 characters long.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsync_SetsHashAndUpdatesUser()
    {
        var token = new Token { UserId = 9, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var user = new User { Id = 9, Email = "user@example.com" };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(9)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask).Verifiable();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash("user@example.comnewpass123")).Returns("hashed");

        var service = CreateService(userRepository: userRepo.Object, tokenRepository: tokenRepo.Object, passwordHasher: hasher.Object);

        var result = await service.SetPasswordWithTokenAsync("tok", "newpass123");

        Assert.True(result.Success);
        Assert.Equal(9, result.UserId);
        Assert.Equal("user@example.com", result.UserEmail);
        Assert.Equal("hashed", user.PasswordHash);
        userRepo.Verify();
    }

    [Fact]
    public async Task SetInitialPasswordAsync_WhenAlreadySet_ReturnsError()
    {
        var user = new User { Id = 2, Email = "u@example.com", PasswordHash = "exists" };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(user);

        var service = CreateService(userRepository: userRepo.Object);

        var result = await service.SetInitialPasswordAsync(2, "whatever123");

        Assert.False(result.Success);
        Assert.Equal("Password already set.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetInitialPasswordAsync_SucceedsAndReturnsTokenAndSlug()
    {
        var user = new User { Id = 4, Email = "u@example.com", PasswordHash = null };
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(4)).ReturnsAsync(user);
        userRepo.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask).Verifiable();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash("u@example.comnewpass123")).Returns("hashed");

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.CreateForUserIdAsync(4)).ReturnsAsync(new Token { Value = "login-token" });

        var uw = new UserWorkspace { WorkspaceId = 10 };
        var uwRepo = new Mock<IUserWorkspaceRepository>();
        uwRepo.Setup(r => r.FindAcceptedForUserAsync(4)).ReturnsAsync(uw);

        var ws = new Workspace { Id = 10, Slug = "ws-slug" };
        var wsRepo = new Mock<IWorkspaceRepository>();
        wsRepo.Setup(r => r.FindByIdAsync(10)).ReturnsAsync(ws);

        var service = CreateService(
            userRepository: userRepo.Object,
            tokenRepository: tokenRepo.Object,
            passwordHasher: hasher.Object,
            userWorkspaceRepository: uwRepo.Object,
            workspaceRepository: wsRepo.Object);

        var result = await service.SetInitialPasswordAsync(4, "newpass123");

        Assert.True(result.Success);
        Assert.Equal("login-token", result.LoginToken);
        Assert.Equal("ws-slug", result.WorkspaceSlug);
        Assert.Equal("hashed", user.PasswordHash);
        userRepo.Verify();
    }

    private static PasswordSetupService CreateService(
        IUserRepository? userRepository = null,
        ITokenRepository? tokenRepository = null,
        IPasswordHasher? passwordHasher = null,
        IUserWorkspaceRepository? userWorkspaceRepository = null,
        IWorkspaceRepository? workspaceRepository = null)
    {
        userRepository ??= new Mock<IUserRepository>().Object;
        tokenRepository ??= new Mock<ITokenRepository>().Object;
        passwordHasher ??= new Mock<IPasswordHasher>().Object;
        userWorkspaceRepository ??= new Mock<IUserWorkspaceRepository>().Object;
        workspaceRepository ??= new Mock<IWorkspaceRepository>().Object;

        return new PasswordSetupService(
            userRepository,
            tokenRepository,
            passwordHasher,
            userWorkspaceRepository,
            workspaceRepository);
    }
}


