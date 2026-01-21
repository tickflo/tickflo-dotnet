namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceRolesViewServiceTests
{
    [Fact]
    public async Task BuildAsyncLoadsRolesWithCountsForAdmin()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var roleService = new Mock<IRoleManagementService>();

        var roles = new List<Role>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "Editor" },
            new() { Id = 2, WorkspaceId = 1, Name = "Viewer" }
        };

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(true);

        roleService.Setup(x => x.GetWorkspaceRolesAsync(1))
            .ReturnsAsync(roles);

        roleService.Setup(x => x.CountRoleAssignmentsAsync(1, 1))
            .ReturnsAsync(5);

        roleService.Setup(x => x.CountRoleAssignmentsAsync(1, 2))
            .ReturnsAsync(3);

        var service = new WorkspaceRolesViewService(accessService.Object, roleService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.IsAdmin);
        Assert.Equal(2, result.Roles.Count);
        Assert.Equal("Editor", result.Roles[0].Name);
        Assert.Equal(5, result.RoleAssignmentCounts[1]);
        Assert.Equal(3, result.RoleAssignmentCounts[2]);
    }

    [Fact]
    public async Task BuildAsyncDeniesNonAdminAccess()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var roleService = new Mock<IRoleManagementService>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        var service = new WorkspaceRolesViewService(accessService.Object, roleService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.IsAdmin);
        Assert.Empty(result.Roles);
        Assert.Empty(result.RoleAssignmentCounts);
        roleService.Verify(x => x.GetWorkspaceRolesAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task BuildAsyncHandlesEmptyRoleList()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var roleService = new Mock<IRoleManagementService>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(true);

        roleService.Setup(x => x.GetWorkspaceRolesAsync(1))
            .ReturnsAsync([]);

        var service = new WorkspaceRolesViewService(accessService.Object, roleService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.IsAdmin);
        Assert.Empty(result.Roles);
        Assert.Empty(result.RoleAssignmentCounts);
    }
}

