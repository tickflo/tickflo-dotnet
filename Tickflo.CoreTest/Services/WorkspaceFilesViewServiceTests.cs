namespace Tickflo.CoreTest.Services;

using Moq;
using Xunit;

public class WorkspaceFilesViewServiceTests
{
    [Fact]
    public async Task BuildAsyncAllowsViewingWhenUserHasAccess()
    {
        var access = new Mock<IWorkspaceAccessService>();
        access.Setup(a => a.UserHasAccessAsync(1, 10)).ReturnsAsync(true);

        var svc = new WorkspaceFilesViewService(access.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.CanViewFiles);
    }

    [Fact]
    public async Task BuildAsyncDeniesWhenUserHasNoAccess()
    {
        var access = new Mock<IWorkspaceAccessService>();
        access.Setup(a => a.UserHasAccessAsync(1, 10)).ReturnsAsync(false);

        var svc = new WorkspaceFilesViewService(access.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.False(result.CanViewFiles);
    }
}

