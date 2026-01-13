using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceTeamsEditViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ForAdmin_GrantsAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockTeamRepo = new Mock<ITeamRepository>();
        var mockUserWorkspaces = new Mock<IUserWorkspaceRepository>();
        var mockUsers = new Mock<IUserRepository>();
        var mockTeamMembers = new Mock<ITeamMemberRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceTeamsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockTeamRepo.Object, mockUserWorkspaces.Object, mockUsers.Object, mockTeamMembers.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.True(result.CanViewTeams);
        Assert.True(result.CanEditTeams);
        Assert.True(result.CanCreateTeams);
    }

    [Fact]
    public async Task BuildAsync_ForNonAdminWithoutPermission_DeniesAllPermissions()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockTeamRepo = new Mock<ITeamRepository>();
        var mockUserWorkspaces = new Mock<IUserWorkspaceRepository>();
        var mockUsers = new Mock<IUserRepository>();
        var mockTeamMembers = new Mock<ITeamMemberRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(2, 1)).ReturnsAsync(false);
        mockRolePerms.Setup(x => x.GetEffectivePermissionsForUserAsync(1, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var service = new WorkspaceTeamsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockTeamRepo.Object, mockUserWorkspaces.Object, mockUsers.Object, mockTeamMembers.Object);

        // Act
        var result = await service.BuildAsync(1, 2, 0);

        // Assert
        Assert.False(result.CanViewTeams);
        Assert.False(result.CanEditTeams);
        Assert.False(result.CanCreateTeams);
    }

    [Fact]
    public async Task BuildAsync_LoadsWorkspaceUsers()
    {
        // Arrange
        var mockUserWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var mockRolePerms = new Mock<IRolePermissionRepository>();
        var mockTeamRepo = new Mock<ITeamRepository>();
        var mockUserWorkspaces = new Mock<IUserWorkspaceRepository>();
        var mockUsers = new Mock<IUserRepository>();
        var mockTeamMembers = new Mock<ITeamMemberRepository>();

        mockUserWorkspaceRoleRepo.Setup(x => x.IsAdminAsync(1, 1)).ReturnsAsync(true);

        var service = new WorkspaceTeamsEditViewService(mockUserWorkspaceRoleRepo.Object, mockRolePerms.Object, mockTeamRepo.Object, mockUserWorkspaces.Object, mockUsers.Object, mockTeamMembers.Object);

        // Act
        var result = await service.BuildAsync(1, 1, 0);

        // Assert
        Assert.NotNull(result.WorkspaceUsers);
    }
}

