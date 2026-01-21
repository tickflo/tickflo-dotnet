namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceServiceTests
{
    [Fact]
    public async Task GetAcceptedWorkspacesAsyncFiltersByAccepted()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindForUserAsync(3)).ReturnsAsync(
        [
            new() { WorkspaceId = 1, Accepted = true },
            new() { WorkspaceId = 2, Accepted = false }
        ]);
        var svc = new WorkspaceService(Mock.Of<IWorkspaceRepository>(), uw.Object);

        var accepted = await svc.GetAcceptedWorkspacesAsync(3);
        Assert.Single(accepted);
        Assert.Equal(1, accepted[0].WorkspaceId);
    }
}
