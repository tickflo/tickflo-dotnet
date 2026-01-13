using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Users;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class UserManagementServiceTests
{
    [Fact]
    public async Task CreateUserAsync_Throws_On_Duplicate_Email()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.FindByEmailAsync("dup@test.com")).ReturnsAsync(new User { Id = 1 });
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var svc = new UserManagementService(repo.Object, hasher.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateUserAsync("Name", "dup@test.com", null, "pwd"));
    }
}
