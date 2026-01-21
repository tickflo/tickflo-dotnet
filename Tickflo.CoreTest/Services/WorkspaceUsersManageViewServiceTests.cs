namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceUsersManageViewServiceTests
{
    [Fact]
    public async Task BuildAsyncAllowsEditWhenAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);

        var svc = new WorkspaceUsersManageViewService(userWorkspaceRoleRepository.Object, perms.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.CanEditUsers);
    }

    [Fact]
    public async Task BuildAsyncAllowsEditWhenEffectivePerms()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "users", new EffectiveSectionPermission { Section = "users", CanEdit = true } }
            });

        var svc = new WorkspaceUsersManageViewService(userWorkspaceRoleRepository.Object, perms.Object);
        var result = await svc.BuildAsync(10, 2);

        Assert.True(result.CanEditUsers);
    }

    [Fact]
    public async Task BuildAsyncDeniesEditWhenNoPerms()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(3, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 3))
            .ReturnsAsync([]);

        var svc = new WorkspaceUsersManageViewService(userWorkspaceRoleRepository.Object, perms.Object);
        var result = await svc.BuildAsync(10, 3);

        Assert.False(result.CanEditUsers);
    }
}

