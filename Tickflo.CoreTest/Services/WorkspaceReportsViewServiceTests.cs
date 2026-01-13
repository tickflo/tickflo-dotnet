using Moq;
using Xunit;
using Tickflo.Core.Data;

namespace Tickflo.CoreTest.Services;

public class WorkspaceReportsViewServiceTests
{
    [Fact]
    public async Task BuildAsync_LoadsReportsAndPermissions()
    {
        // Arrange
        var rolePerms = new Mock<IRolePermissionRepository>();
        var reportQuery = new Mock<IReportQueryService>();
        var accessService = new Mock<IWorkspaceAccessService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "reports", new EffectiveSectionPermission { Section = "reports", CanCreate = true, CanEdit = true } }
        };

        var reports = new List<ReportListItem>
        {
            new ReportListItem(1, "Monthly Sales", true, DateTime.UtcNow.AddDays(-1)),
            new ReportListItem(2, "Q4 Summary", false, null)
        };

        rolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 100))
            .ReturnsAsync(permissions);

        reportQuery.Setup(x => x.ListReportsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        var service = new WorkspaceReportsViewService(rolePerms.Object, reportQuery.Object, accessService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.CanCreateReports);
        Assert.True(result.CanEditReports);
        Assert.Equal(2, result.Reports.Count);
        Assert.Equal("Monthly Sales", result.Reports[0].Name);
        Assert.True(result.Reports[0].Ready);
        Assert.Equal("Q4 Summary", result.Reports[1].Name);
        Assert.False(result.Reports[1].Ready);
    }

    [Fact]
    public async Task BuildAsync_DefaultsPermissionsWhenNotFound()
    {
        // Arrange
        var rolePerms = new Mock<IRolePermissionRepository>();
        var reportQuery = new Mock<IReportQueryService>();
        var accessService = new Mock<IWorkspaceAccessService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        rolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 100))
            .ReturnsAsync(permissions);

        reportQuery.Setup(x => x.ListReportsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReportListItem>());

        var service = new WorkspaceReportsViewService(rolePerms.Object, reportQuery.Object, accessService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateReports);
        Assert.False(result.CanEditReports);
        Assert.Empty(result.Reports);
    }

    [Fact]
    public async Task BuildAsync_HandlesEmptyReportsList()
    {
        // Arrange
        var rolePerms = new Mock<IRolePermissionRepository>();
        var reportQuery = new Mock<IReportQueryService>();
        var accessService = new Mock<IWorkspaceAccessService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "reports", new EffectiveSectionPermission { Section = "reports", CanCreate = false, CanEdit = false } }
        };

        rolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 100))
            .ReturnsAsync(permissions);

        reportQuery.Setup(x => x.ListReportsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReportListItem>());

        var service = new WorkspaceReportsViewService(rolePerms.Object, reportQuery.Object, accessService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateReports);
        Assert.False(result.CanEditReports);
        Assert.Empty(result.Reports);
    }
}

