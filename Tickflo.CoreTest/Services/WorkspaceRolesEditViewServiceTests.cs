namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceRolesEditViewServiceTests
{
    [Fact]
    public async Task BuildAsyncForAdminUserReturnsAdminFlag()
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
    public async Task BuildAsyncForNonAdminUserReturnsNonAdminFlag()
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
    public async Task BuildAsyncNonAdminDeniesAllAccess()
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

