using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Views;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceReportRunsBackfillViewServiceTests
{
    [Fact]
    public async Task BuildAsync_Sets_CanEdit_When_Admin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        var perms = new Mock<IRolePermissionRepository>();
        var svc = new WorkspaceReportRunsBackfillViewService(uwr.Object, perms.Object);

        var data = await svc.BuildAsync(1, 5);
        Assert.True(data.CanEditReports);
    }
}
