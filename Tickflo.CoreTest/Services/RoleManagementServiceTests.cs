using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Roles;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class RoleManagementServiceTests
{
    [Fact]
    public async Task AssignRoleToUserAsync_Throws_When_Role_Not_In_Workspace()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.FindByIdAsync(5)).ReturnsAsync(new Role { Id = 5, WorkspaceId = 2 });
        var svc = new RoleManagementService(uwr.Object, roles.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AssignRoleToUserAsync(1, 1, 5, 9));
    }

    [Fact]
    public async Task CountRoleAssignmentsAsync_Delegates()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.CountAssignmentsForRoleAsync(1, 2)).ReturnsAsync(3);
        var svc = new RoleManagementService(uwr.Object, Mock.Of<IRoleRepository>());

        var count = await svc.CountRoleAssignmentsAsync(1, 2);
        Assert.Equal(3, count);
    }
}
