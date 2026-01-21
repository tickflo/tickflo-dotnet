namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceReportRunsBackfillViewServiceTests
{
    [Fact]
    public async Task BuildAsyncSetsCanEditWhenAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        var perms = new Mock<IRolePermissionRepository>();
        var svc = new WorkspaceReportRunsBackfillViewService(userWorkspaceRoleRepository.Object, perms.Object);

        var data = await svc.BuildAsync(1, 5);
        Assert.True(data.CanEditReports);
    }
}
