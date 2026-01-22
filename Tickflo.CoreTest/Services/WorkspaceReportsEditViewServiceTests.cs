namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceReportsEditViewServiceTests
{
    [Fact]
    public async Task BuildAsyncForAdminGrantsAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockReportRepo = new Mock<IReportRepository>();
        var mockReportingService = new Mock<IReportingService>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceReportsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockReportRepo.Object, mockReportingService.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.True(result.CanViewReports);
        Assert.True(result.CanEditReports);
        Assert.True(result.CanCreateReports);
    }

    [Fact]
    public async Task BuildAsyncForNonAdminWithoutPermissionDeniesAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockReportRepo = new Mock<IReportRepository>();
        var mockReportingService = new Mock<IReportingService>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);
        mockRolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 2))
            .ReturnsAsync([]);

        var service = new WorkspaceReportsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockReportRepo.Object, mockReportingService.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.CanViewReports);
        Assert.False(result.CanEditReports);
        Assert.False(result.CanCreateReports);
    }

    private static readonly string[] Value = ["id", "name"];

    [Fact]
    public async Task BuildAsyncLoadsAvailableSources()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockReportRepo = new Mock<IReportRepository>();
        var mockReportingService = new Mock<IReportingService>();

        var sources = new Dictionary<string, string[]> { { "tickets", Value } };
        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);
        mockReportingService.Setup(x => x.GetAvailableSources()).Returns(sources);

        var service = new WorkspaceReportsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockReportRepo.Object, mockReportingService.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.NotEmpty(result.Sources);
    }
}

