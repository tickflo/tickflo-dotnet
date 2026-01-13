using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Services;

namespace Tickflo.CoreTest.Services;

public class WorkspaceRolesEditViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ForAdminUser_ReturnsAdminFlag()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRoleRepo = new Mock<IRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceRolesEditViewService(mockUserWorkspaceRoleRepo.Object, mockRoleRepo.Object, mockRolePerms.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.True(result.IsAdmin);
    }

    [Fact]
    public async Task BuildAsync_ForNonAdminUser_ReturnsNonAdminFlag()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRoleRepo = new Mock<IRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);

        var service = new WorkspaceRolesEditViewService(mockUserWorkspaceRoleRepo.Object, mockRoleRepo.Object, mockRolePerms.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.IsAdmin);
    }

    [Fact]
    public async Task BuildAsync_NonAdminDeniesAllAccess()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRoleRepo = new Mock<IRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);

        var service = new WorkspaceRolesEditViewService(mockUserWorkspaceRoleRepo.Object, mockRoleRepo.Object, mockRolePerms.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 1);

        // Assert
        Assert.Null(result.ExistingRole);
        Assert.Empty(result.ExistingPermissions);
    }
}
