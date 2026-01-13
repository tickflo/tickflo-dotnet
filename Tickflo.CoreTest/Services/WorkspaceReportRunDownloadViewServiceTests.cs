using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceReportRunDownloadViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ReturnsRun_WhenUserCanViewAndRunMatches()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reportRunService = new Mock<IReportRunService>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 1))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "reports", new EffectiveSectionPermission { Section = "reports", CanView = true } }
            });

        var run = new ReportRun { Id = 7, WorkspaceId = 10, ReportId = 5, Status = "completed" };
        reportRunService.Setup(x => x.GetRunAsync(10, 7, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(run);

        var svc = new WorkspaceReportRunDownloadViewService(uwr.Object, perms.Object, reportRunService.Object);
        var result = await svc.BuildAsync(10, 1, 5, 7);

        Assert.True(result.CanViewReports);
        Assert.NotNull(result.Run);
        Assert.Equal(7, result.Run!.Id);
        Assert.Equal(5, result.Run!.ReportId);
    }

    [Fact]
    public async Task BuildAsync_Denies_WhenUserCannotView()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reportRunService = new Mock<IReportRunService>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 1))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var svc = new WorkspaceReportRunDownloadViewService(uwr.Object, perms.Object, reportRunService.Object);
        var result = await svc.BuildAsync(10, 1, 5, 7);

        Assert.False(result.CanViewReports);
        Assert.Null(result.Run);
    }

    [Fact]
    public async Task BuildAsync_EmptyRun_WhenRunDoesNotMatchReport()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reportRunService = new Mock<IReportRunService>();

        uwr.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "reports", new EffectiveSectionPermission { Section = "reports", CanView = true } }
            });

        var run = new ReportRun { Id = 8, WorkspaceId = 10, ReportId = 6, Status = "completed" };
        reportRunService.Setup(x => x.GetRunAsync(10, 8, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(run);

        var svc = new WorkspaceReportRunDownloadViewService(uwr.Object, perms.Object, reportRunService.Object);
        var result = await svc.BuildAsync(10, 2, 5, 8);

        Assert.True(result.CanViewReports);
        Assert.Null(result.Run);
    }
}

