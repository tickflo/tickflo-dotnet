namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceUsersInviteViewServiceTests
{
    [Fact]
    public async Task BuildAsyncAllowsViewAndCreateWhenAdmin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);

        var svc = new WorkspaceUsersInviteViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.CanViewUsers);
        Assert.True(result.CanCreateUsers);
    }

    [Fact]
    public async Task BuildAsyncAllowsViewAndCreateWhenEffectivePerms()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "users", new EffectiveSectionPermission { Section = "users", CanView = true, CanCreate = true } }
            });

        var svc = new WorkspaceUsersInviteViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 2);

        Assert.True(result.CanViewUsers);
        Assert.True(result.CanCreateUsers);
    }

    [Fact]
    public async Task BuildAsyncDeniesViewAndCreateWhenNoPerms()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(3, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 3))
            .ReturnsAsync([]);

        var svc = new WorkspaceUsersInviteViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 3);

        Assert.False(result.CanViewUsers);
        Assert.False(result.CanCreateUsers);
    }

    [Fact]
    public async Task BuildAsyncAllowsViewOnlyWhenCanViewButNotCreate()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        uwr.Setup(x => x.IsAdminAsync(4, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 4))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "users", new EffectiveSectionPermission { Section = "users", CanView = true, CanCreate = false } }
            });

        var svc = new WorkspaceUsersInviteViewService(uwr.Object, perms.Object);
        var result = await svc.BuildAsync(10, 4);

        Assert.True(result.CanViewUsers);
        Assert.False(result.CanCreateUsers);
    }
}

