using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Workspace;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceAccessServiceTests
{
    [Fact]
    public async Task EnsureAdminAccessAsync_Throws_When_Not_Admin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(false);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), uwr.Object, Mock.Of<IRolePermissionRepository>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.EnsureAdminAccessAsync(5, 1));
    }
}
