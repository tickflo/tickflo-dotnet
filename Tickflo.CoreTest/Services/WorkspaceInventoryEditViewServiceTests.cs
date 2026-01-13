using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceInventoryEditViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ForAdmin_GrantsAllPermissions()
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
    public async Task BuildAsync_ForNonAdminWithoutPermission_DeniesAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockInventoryRepo = new Mock<IInventoryRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);
        mockRolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var service = new WorkspaceInventoryEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockInventoryRepo.Object, mockLocationRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.CanViewInventory);
        Assert.False(result.CanEditInventory);
        Assert.False(result.CanCreateInventory);
    }

    [Fact]
    public async Task BuildAsync_CreatesDefaultItemForNewEntry()
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

