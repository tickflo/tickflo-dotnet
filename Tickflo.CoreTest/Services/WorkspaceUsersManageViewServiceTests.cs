using Moq;
using Xunit;
using Tickflo.Core.Services;
using Tickflo.Core.Data;

namespace Tickflo.CoreTest.Services;

public class WorkspaceUsersManageViewServiceTests
{
    [Fact]
    public async Task BuildAsync_AllowsEdit_WhenAdmin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);

        var svc = new WorkspaceUsersManageViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.CanEditUsers);
    }

    [Fact]
    public async Task BuildAsync_AllowsEdit_WhenEffectivePerms()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "users", new EffectiveSectionPermission { Section = "users", CanEdit = true } }
            });

        var svc = new WorkspaceUsersManageViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 2);

        Assert.True(result.CanEditUsers);
    }

    [Fact]
    public async Task BuildAsync_DeniesEdit_WhenNoPerms()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(3, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 3))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var svc = new WorkspaceUsersManageViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 3);

        Assert.False(result.CanEditUsers);
    }
}
