namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class UserManagementServiceTests
{
    private static IUserManagementService CreateService(
        IUserRepository? userRepository = null,
        IPasswordHasher? hasher = null)
    {
        userRepository ??= Mock.Of<IUserRepository>();
        hasher ??= new MockPasswordHasher();
        return new UserManagementService(userRepository, hasher);
    }

    [Fact]
    public async Task CreateUserAsyncThrowsOnDuplicateEmail()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByEmailAsync("dup@test.com")).ReturnsAsync(new User { Id = 1 });
        var svc = CreateService(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateUserAsync("Name", "dup@test.com", null, "pwd"));
    }

    [Fact]
    public async Task CreateUserAsyncNormalizesEmail()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);

        var svc = CreateService(repo.Object);
        await svc.CreateUserAsync("Name", "Test@EXAMPLE.COM", null, "pwd");

        repo.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "test@example.com")), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsyncThrowsWhenUserNotFound()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync((User)null!);
        var svc = CreateService(repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.UpdateUserAsync(1, "Name", "test@example.com", null));
    }

    [Fact]
    public async Task IsEmailInUseAsyncReturnsFalseWhenNotFound()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
        var svc = CreateService(repo.Object);

        var result = await svc.IsEmailInUseAsync("test@example.com");

        Assert.False(result);
    }

    [Fact]
    public async Task IsEmailInUseAsyncReturnsTrueWhenFound()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(new User { Id = 1 });
        var svc = CreateService(repo.Object);

        var result = await svc.IsEmailInUseAsync("test@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task IsEmailInUseAsyncReturnsFalseWhenExcludedUser()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(new User { Id = 1 });
        var svc = CreateService(repo.Object);

        var result = await svc.IsEmailInUseAsync("test@example.com", excludeUserId: 1);

        Assert.False(result);
    }

    private sealed class MockPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hash_{password}";
        public bool Verify(string password, string hash) => hash == $"hash_{password}";
    }
}
