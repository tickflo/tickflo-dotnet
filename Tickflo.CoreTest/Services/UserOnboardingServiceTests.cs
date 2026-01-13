using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Users;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class UserOnboardingServiceTests
{
    [Fact]
    public async Task InviteUserToWorkspaceAsync_Throws_For_Invalid_Email()
    {
        var svc = new UserOnboardingService(Mock.Of<IUserRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IWorkspaceRepository>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.InviteUserToWorkspaceAsync(1, "bad", 1, 2));
    }
}
