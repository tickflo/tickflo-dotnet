using Moq;
using Xunit;
using Tickflo.Core.Services;
using Tickflo.Core.Data;

namespace Tickflo.CoreTest.Services;

public class WorkspaceFilesViewServiceTests
{
    [Fact]
    public async Task BuildAsync_AllowsViewingWhenUserHasAccess()
    {
        var access = new Mock<IWorkspaceAccessService>();
        access.Setup(a => a.UserHasAccessAsync(1, 10)).ReturnsAsync(true);

        var svc = new WorkspaceFilesViewService(access.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.CanViewFiles);
    }

    [Fact]
    public async Task BuildAsync_DeniesWhenUserHasNoAccess()
    {
        var access = new Mock<IWorkspaceAccessService>();
        access.Setup(a => a.UserHasAccessAsync(1, 10)).ReturnsAsync(false);

        var svc = new WorkspaceFilesViewService(access.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.False(result.CanViewFiles);
    }
}
