namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class UserOnboardingServiceTests
{
    [Fact]
    public async Task InviteUserToWorkspaceAsyncThrowsForInvalidEmail()
    {
        var svc = new UserOnboardingService(Mock.Of<IUserRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IWorkspaceRepository>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.InviteUserToWorkspaceAsync(1, "bad", 1, 2));
    }
}
