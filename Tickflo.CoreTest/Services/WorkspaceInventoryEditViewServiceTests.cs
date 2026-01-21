namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceInventoryEditViewServiceTests
{
    [Fact]
    public async Task BuildAsyncForAdminGrantsAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockInventoryRepo = new Mock<IInventoryRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceInventoryEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockInventoryRepo.Object, mockLocationRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.True(result.CanViewInventory);
        Assert.True(result.CanEditInventory);
        Assert.True(result.CanCreateInventory);
    }

    [Fact]
    public async Task BuildAsyncForNonAdminWithoutPermissionDeniesAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockInventoryRepo = new Mock<IInventoryRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);
        mockRolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 2))
            .ReturnsAsync([]);

        var service = new WorkspaceInventoryEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockInventoryRepo.Object, mockLocationRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.CanViewInventory);
        Assert.False(result.CanEditInventory);
        Assert.False(result.CanCreateInventory);
    }

    [Fact]
    public async Task BuildAsyncCreatesDefaultItemForNewEntry()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockInventoryRepo = new Mock<IInventoryRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceInventoryEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockInventoryRepo.Object, mockLocationRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.NotNull(result.ExistingItem);
        Assert.Equal("active", result.ExistingItem.Status);
    }
}

