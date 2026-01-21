namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceReportsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncLoadsReportsAndPermissions()
    {
        // Arrange
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        var reportQuery = new Mock<IReportQueryService>();
        var accessService = new Mock<IWorkspaceAccessService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "reports", new EffectiveSectionPermission { Section = "reports", CanCreate = true, CanEdit = true } }
        };

        var reports = new List<ReportListItem>
        {
            new(1, "Monthly Sales", true, DateTime.UtcNow.AddDays(-1)),
            new(2, "Q4 Summary", false, null)
        };

        rolePermissionRepository.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 100))
            .ReturnsAsync(permissions);

        reportQuery.Setup(x => x.ListReportsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        var service = new WorkspaceReportsViewService(rolePermissionRepository.Object, reportQuery.Object, accessService.Object);

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
    public async Task BuildAsyncDefaultsPermissionsWhenNotFound()
    {
        // Arrange
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        var reportQuery = new Mock<IReportQueryService>();
        var accessService = new Mock<IWorkspaceAccessService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        rolePermissionRepository.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 100))
            .ReturnsAsync(permissions);

        reportQuery.Setup(x => x.ListReportsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new WorkspaceReportsViewService(rolePermissionRepository.Object, reportQuery.Object, accessService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateReports);
        Assert.False(result.CanEditReports);
        Assert.Empty(result.Reports);
    }

    [Fact]
    public async Task BuildAsyncHandlesEmptyReportsList()
    {
        // Arrange
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        var reportQuery = new Mock<IReportQueryService>();
        var accessService = new Mock<IWorkspaceAccessService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "reports", new EffectiveSectionPermission { Section = "reports", CanCreate = false, CanEdit = false } }
        };

        rolePermissionRepository.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 100))
            .ReturnsAsync(permissions);

        reportQuery.Setup(x => x.ListReportsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = new WorkspaceReportsViewService(rolePermissionRepository.Object, reportQuery.Object, accessService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateReports);
        Assert.False(result.CanEditReports);
        Assert.Empty(result.Reports);
    }
}

