using Moq;
using Xunit;
using Tickflo.Core.Services;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceRolesAssignViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ReturnsMembersRoles_WhenAdmin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var userWorkspaces = new Mock<IUserWorkspaceRepository>();
        var users = new Mock<IUserRepository>();
        var roles = new Mock<IRoleRepository>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);

        userWorkspaces.Setup(x => x.FindForWorkspaceAsync(10))
            .ReturnsAsync(new List<UserWorkspace>
            {
                new UserWorkspace { UserId = 2, WorkspaceId = 10, Accepted = true },
                new UserWorkspace { UserId = 3, WorkspaceId = 10, Accepted = true }
            });

        users.Setup(x => x.FindByIdAsync(2)).ReturnsAsync(new User { Id = 2, Name = "Alice" });
        users.Setup(x => x.FindByIdAsync(3)).ReturnsAsync(new User { Id = 3, Name = "Bob" });

        roles.Setup(x => x.ListForWorkspaceAsync(10))
            .ReturnsAsync(new List<Role> { new Role { Id = 7, WorkspaceId = 10, Name = "Manager" } });

        uwr.Setup(x => x.GetRolesAsync(2, 10))
            .ReturnsAsync(new List<Role> { new Role { Id = 7, WorkspaceId = 10, Name = "Manager" } });
        uwr.Setup(x => x.GetRolesAsync(3, 10))
            .ReturnsAsync(new List<Role>());

        var svc = new WorkspaceRolesAssignViewService(uwr.Object, userWorkspaces.Object, users.Object, roles.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.IsAdmin);
        Assert.Equal(2, result.Members.Count);
        Assert.Single(result.Roles);
        Assert.True(result.UserRoles.ContainsKey(2));
        Assert.Single(result.UserRoles[2]);
        Assert.True(result.UserRoles.ContainsKey(3));
        Assert.Empty(result.UserRoles[3]);
    }

    [Fact]
    public async Task BuildAsync_DeniesWhenNotAdmin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var userWorkspaces = new Mock<IUserWorkspaceRepository>();
        var users = new Mock<IUserRepository>();
        var roles = new Mock<IRoleRepository>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(false);

        var svc = new WorkspaceRolesAssignViewService(uwr.Object, userWorkspaces.Object, users.Object, roles.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.False(result.IsAdmin);
        Assert.Empty(result.Members);
        Assert.Empty(result.Roles);
        Assert.Empty(result.UserRoles);
    }
}
