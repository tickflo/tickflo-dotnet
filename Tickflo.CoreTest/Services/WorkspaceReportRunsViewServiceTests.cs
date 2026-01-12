using Moq;
using Xunit;
using Tickflo.Core.Services;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceReportRunsViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ReturnsReportAndRuns_WhenUserCanView()
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

        var report = new Report { Id = 5, WorkspaceId = 10, Name = "Test", Ready = true, DefinitionJson = "{}" };
        var runs = new List<ReportRun>
        {
            new ReportRun { Id = 7, ReportId = 5, WorkspaceId = 10, Status = "completed" },
            new ReportRun { Id = 8, ReportId = 5, WorkspaceId = 10, Status = "running" }
        };

        reportRunService.Setup(x => x.GetReportRunsAsync(10, 5, 100, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((report, (IReadOnlyList<ReportRun>)runs));

        var svc = new WorkspaceReportRunsViewService(uwr.Object, perms.Object, reportRunService.Object);
        var result = await svc.BuildAsync(10, 1, 5);

        Assert.True(result.CanViewReports);
        Assert.NotNull(result.Report);
        Assert.Equal(5, result.Report!.Id);
        Assert.Equal(2, result.Runs.Count);
    }

    [Fact]
    public async Task BuildAsync_Denies_WhenUserCannotView()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reportRunService = new Mock<IReportRunService>();

        uwr.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var svc = new WorkspaceReportRunsViewService(uwr.Object, perms.Object, reportRunService.Object);
        var result = await svc.BuildAsync(10, 2, 5);

        Assert.False(result.CanViewReports);
        Assert.Null(result.Report);
        Assert.Empty(result.Runs);
    }

    [Fact]
    public async Task BuildAsync_Empty_WhenReportNotFound()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reportRunService = new Mock<IReportRunService>();

        uwr.Setup(x => x.IsAdminAsync(3, 10)).ReturnsAsync(true);

        reportRunService.Setup(x => x.GetReportRunsAsync(10, 5, 100, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(((Report?)null, (IReadOnlyList<ReportRun>)new List<ReportRun>()));

        var svc = new WorkspaceReportRunsViewService(uwr.Object, perms.Object, reportRunService.Object);
        var result = await svc.BuildAsync(10, 3, 5);

        Assert.True(result.CanViewReports);
        Assert.Null(result.Report);
        Assert.Empty(result.Runs);
    }
}
