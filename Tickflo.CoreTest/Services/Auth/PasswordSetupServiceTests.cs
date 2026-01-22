namespace Tickflo.CoreTest.Services.Auth;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class PasswordSetupServiceTests
{
    [Fact]
    public async Task ValidateResetTokenAsyncMissingTokenReturnsError()
    {
        var service = CreateService();

        var result = await service.ValidateResetTokenAsync(string.Empty);

        Assert.False(result.IsValid);
        Assert.Equal("Missing token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateResetTokenAsyncInvalidTokenReturnsError()
    {
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("bad")).ReturnsAsync((Token?)null);
        var service = CreateService(tokenRepository: tokenRepo.Object);

        var result = await service.ValidateResetTokenAsync("bad");

        Assert.False(result.IsValid);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateResetTokenAsyncUserMissingReturnsError()
    {
        var token = new Token { UserId = 7, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.FindByIdAsync(7)).ReturnsAsync((User?)null);

        var service = CreateService(userRepository: userRepository.Object, tokenRepository: tokenRepo.Object);

        var result = await service.ValidateResetTokenAsync("tok");

        Assert.False(result.IsValid);
        Assert.Equal("User not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateResetTokenAsyncValidReturnsUser()
    {
        var token = new Token { UserId = 3, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var user = new User { Id = 3, Email = "u@example.com" };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.FindByIdAsync(3)).ReturnsAsync(user);

        var service = CreateService(userRepository: userRepository.Object, tokenRepository: tokenRepo.Object);

        var result = await service.ValidateResetTokenAsync("tok");

        Assert.True(result.IsValid);
        Assert.Equal(3, result.UserId);
        Assert.Equal("u@example.com", result.UserEmail);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsyncInvalidTokenReturnsError()
    {
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync((Token?)null);

        var service = CreateService(tokenRepository: tokenRepo.Object);

        var result = await service.SetPasswordWithTokenAsync("tok", "newpass123");

        Assert.False(result.Success);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsyncTooShortReturnsError()
    {
        var token = new Token { UserId = 5, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var user = new User { Id = 5, Email = "short@example.com" };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(user);

        var service = CreateService(userRepository: userRepository.Object, tokenRepository: tokenRepo.Object);

        var result = await service.SetPasswordWithTokenAsync("tok", "short");

        Assert.False(result.Success);
        Assert.Equal("Password must be at least 8 characters long.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetPasswordWithTokenAsyncSetsHashAndUpdatesUser()
    {
        var token = new Token { UserId = 9, Value = "tok" };
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.FindByValueAsync("tok")).ReturnsAsync(token);

        var user = new User { Id = 9, Email = "user@example.com" };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.FindByIdAsync(9)).ReturnsAsync(user);
        userRepository.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask).Verifiable();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash("user@example.comnewpass123")).Returns("hashed");

        var service = CreateService(userRepository: userRepository.Object, tokenRepository: tokenRepo.Object, passwordHasher: hasher.Object);

        var result = await service.SetPasswordWithTokenAsync("tok", "newpass123");

        Assert.True(result.Success);
        Assert.Equal(9, result.UserId);
        Assert.Equal("user@example.com", result.UserEmail);
        Assert.Equal("hashed", user.PasswordHash);
        userRepository.Verify();
    }

    [Fact]
    public async Task SetInitialPasswordAsyncWhenAlreadySetReturnsError()
    {
        var user = new User { Id = 2, Email = "u@example.com", PasswordHash = "exists" };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(user);

        var service = CreateService(userRepository: userRepository.Object);

        var result = await service.SetInitialPasswordAsync(2, "whatever123");

        Assert.False(result.Success);
        Assert.Equal("Password already set.", result.ErrorMessage);
    }

    [Fact]
    public async Task SetInitialPasswordAsyncSucceedsAndReturnsTokenAndSlug()
    {
        var user = new User { Id = 4, Email = "u@example.com", PasswordHash = null };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.FindByIdAsync(4)).ReturnsAsync(user);
        userRepository.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask).Verifiable();

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash("u@example.comnewpass123")).Returns("hashed");

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(r => r.CreateForUserIdAsync(4)).ReturnsAsync(new Token { Value = "login-token" });

        var userWorkspace = new UserWorkspace { WorkspaceId = 10 };
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        userWorkspaceRepository.Setup(r => r.FindAcceptedForUserAsync(4)).ReturnsAsync(userWorkspace);

        var workspace = new Workspace { Id = 10, Slug = "ws-slug" };
        var wsRepo = new Mock<IWorkspaceRepository>();
        wsRepo.Setup(r => r.FindByIdAsync(10)).ReturnsAsync(workspace);

        var service = CreateService(
            userRepository: userRepository.Object,
            tokenRepository: tokenRepo.Object,
            passwordHasher: hasher.Object,
            userWorkspaceRepository: userWorkspaceRepository.Object,
            workspaceRepository: wsRepo.Object);

        var result = await service.SetInitialPasswordAsync(4, "newpass123");

        Assert.True(result.Success);
        Assert.Equal("login-token", result.LoginToken);
        Assert.Equal("ws-slug", result.WorkspaceSlug);
        Assert.Equal("hashed", user.PasswordHash);
        userRepository.Verify();
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


