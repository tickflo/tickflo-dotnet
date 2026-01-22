namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceContactsEditViewServiceTests
{
    [Fact]
    public async Task BuildAsyncForAdminGrantsAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockContactRepo = new Mock<IContactRepository>();
        var mockPriorityRepo = new Mock<ITicketPriorityRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceContactsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockContactRepo.Object, mockPriorityRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.True(result.CanViewContacts);
        Assert.True(result.CanEditContacts);
        Assert.True(result.CanCreateContacts);
    }

    [Fact]
    public async Task BuildAsyncForNonAdminWithoutPermissionDeniesAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockContactRepo = new Mock<IContactRepository>();
        var mockPriorityRepo = new Mock<ITicketPriorityRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);
        mockRolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 2))
            .ReturnsAsync([]);

        var service = new WorkspaceContactsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockContactRepo.Object, mockPriorityRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.CanViewContacts);
        Assert.False(result.CanEditContacts);
        Assert.False(result.CanCreateContacts);
    }

    [Fact]
    public async Task BuildAsyncLoadsPrioritiesList()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockContactRepo = new Mock<IContactRepository>();
        var mockPriorityRepo = new Mock<ITicketPriorityRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceContactsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockContactRepo.Object, mockPriorityRepo.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.NotNull(result.Priorities);
    }
}

