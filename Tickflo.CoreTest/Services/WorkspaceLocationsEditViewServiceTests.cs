using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceLocationsEditViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ForAdmin_GrantsAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();
        var mockUserWorkspaces = new Mock<IUserWorkspaceRepository>();
        var mockUsers = new Mock<IUserRepository>();
        var mockContacts = new Mock<IContactRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceLocationsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockLocationRepo.Object, mockUserWorkspaces.Object, mockUsers.Object, mockContacts.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.True(result.CanViewLocations);
        Assert.True(result.CanEditLocations);
        Assert.True(result.CanCreateLocations);
    }

    [Fact]
    public async Task BuildAsync_ForNonAdminWithoutPermission_DeniesAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();
        var mockUserWorkspaces = new Mock<IUserWorkspaceRepository>();
        var mockUsers = new Mock<IUserRepository>();
        var mockContacts = new Mock<IContactRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);
        mockRolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var service = new WorkspaceLocationsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockLocationRepo.Object, mockUserWorkspaces.Object, mockUsers.Object, mockContacts.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.CanViewLocations);
        Assert.False(result.CanEditLocations);
        Assert.False(result.CanCreateLocations);
    }

    [Fact]
    public async Task BuildAsync_CreatesDefaultLocationForNewEntry()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockLocationRepo = new Mock<ILocationRepository>();
        var mockUserWorkspaces = new Mock<IUserWorkspaceRepository>();
        var mockUsers = new Mock<IUserRepository>();
        var mockContacts = new Mock<IContactRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceLocationsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockLocationRepo.Object, mockUserWorkspaces.Object, mockUsers.Object, mockContacts.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.NotNull(result.ExistingLocation);
        Assert.True(result.ExistingLocation.Active);
    }
}

