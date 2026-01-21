namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceReportRunViewServiceTests
{
    [Fact]
    public async Task BuildAsyncReturnsPagedDataWhenUserCanView()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reports = new Mock<IReportRepository>();
        var runs = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 1))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "reports", new EffectiveSectionPermission { Section = "reports", CanView = true } }
            });

        var report = new Report { Id = 5, WorkspaceId = 10, Name = "Test", Ready = true, DefinitionJson = "{}" };
        reports.Setup(r => r.FindAsync(10, 5)).ReturnsAsync(report);

        var run = new ReportRun { Id = 7, WorkspaceId = 10, ReportId = 5, Status = "completed", RowCount = 2 };
        runs.Setup(rr => rr.FindAsync(10, 7)).ReturnsAsync(run);

        var page = new ReportRunPage(
            Page: 1,
            Take: 50,
            TotalRows: 2,
            TotalPages: 1,
            FromRow: 1,
            ToRow: 2,
            HasContent: true,
            Headers: ["Id", "Subject"],
            Rows:
            [
                ["1", "Foo"],
                ["2", "Bar"]
            ]
        );

        reporting.Setup(x => x.GetRunPageAsync(run, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var svc = new WorkspaceReportRunViewService(userWorkspaceRoleRepository.Object, perms.Object, reports.Object, runs.Object, reporting.Object);
        var result = await svc.BuildAsync(10, 1, 5, 7, 1, 50);

        Assert.True(result.CanViewReports);
        Assert.NotNull(result.Report);
        Assert.NotNull(result.Run);
        Assert.NotNull(result.PageData);
        Assert.Equal(2, result.PageData!.TotalRows);
        Assert.Equal(2, result.PageData!.Rows.Count);
        Assert.Equal("Subject", result.PageData!.Headers[1]);
    }

    [Fact]
    public async Task BuildAsyncDeniesWhenUserCannotView()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var reports = new Mock<IReportRepository>();
        var runs = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 1))
            .ReturnsAsync([]);

        var svc = new WorkspaceReportRunViewService(userWorkspaceRoleRepository.Object, perms.Object, reports.Object, runs.Object, reporting.Object);
        var result = await svc.BuildAsync(10, 1, 5, 7, 1, 50);

        Assert.False(result.CanViewReports);
        Assert.Null(result.Report);
        Assert.Null(result.Run);
        Assert.Null(result.PageData);
    }
}

