using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Users;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class UserInvitationServiceTests
{
    [Fact]
    public async Task InviteUserAsync_Assigns_Roles_When_Provided()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.FindByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        users.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserWorkspace?)null);
        uw.Setup(r => r.AddAsync(It.IsAny<UserWorkspace>())).Returns(Task.CompletedTask);
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Role { Id = 1, WorkspaceId = 2 });
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var svc = new UserInvitationService(users.Object, uw.Object, uwr.Object, roles.Object, hasher.Object);

        var result = await svc.InviteUserAsync(2, "new@test.com", 9, new List<int> { 1 });

        Assert.NotNull(result.User);
        uwr.Verify(r => r.AddAsync(It.IsAny<int>(), 2, 1, 9), Times.Once);
    }
}
