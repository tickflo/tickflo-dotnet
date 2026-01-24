namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Xunit;

public class UserInvitationServiceTests
{
    [Fact]
    public async Task InviteUserAsyncAssignsRolesWhenProvided()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.FindByEmailAsync("new@test.com")).ReturnsAsync((User?)null);
        users.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((UserWorkspace?)null);
        uw.Setup(r => r.AddAsync(It.IsAny<UserWorkspace>())).Returns(Task.CompletedTask);
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.AddAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Role { Id = 1, WorkspaceId = 2 });
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var emailSendService = new Mock<IEmailSendService>();
        emailSendService.Setup(e => e.AddToQueueAsync(
            It.IsAny<string>(),
            It.IsAny<EmailTemplateType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<int?>())).ReturnsAsync(new Email());
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        workspaceRepository.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(new Workspace { Id = 2, Name = "Test Workspace" });
        var svc = new UserInvitationService(
            users.Object,
            uw.Object,
            userWorkspaceRoleRepository.Object,
            roles.Object,
            hasher.Object,
            emailSendService.Object,
            workspaceRepository.Object);

        var result = await svc.InviteUserAsync(2, "new@test.com", 9, [1]);

        Assert.NotNull(result.User);
        userWorkspaceRoleRepository.Verify(r => r.AddAsync(It.IsAny<int>(), 2, 1, 9), Times.Once);
        emailSendService.Verify(e => e.AddToQueueAsync(
            It.IsAny<string>(),
            EmailTemplateType.WorkspaceInviteNewUser,
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<int?>()), Times.Once);
    }
}
