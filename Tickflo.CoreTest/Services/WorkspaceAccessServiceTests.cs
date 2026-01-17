using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceAccessServiceTests
{
    [Fact]
    public async Task EnsureAdminAccessAsync_Throws_When_Not_Admin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(false);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), uwr.Object, Mock.Of<IRolePermissionRepository>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.EnsureAdminAccessAsync(5, 1));
    }

    [Fact]
    public async Task EnsureAdminAccessAsync_Succeeds_For_Admin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), uwr.Object, Mock.Of<IRolePermissionRepository>());

        // Should not throw
        await svc.EnsureAdminAccessAsync(5, 1);
        uwr.Verify(r => r.IsAdminAsync(5, 1), Times.Once);
    }

    [Fact]
    public async Task UserHasAccessAsync_Returns_True_For_Accepted_Member()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync(new UserWorkspace { UserId = 5, WorkspaceId = 1, Accepted = true });
        var svc = new WorkspaceAccessService(uw.Object, Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserHasAccessAsync(5, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasAccessAsync_Returns_False_For_NonAccepted_Member()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync(new UserWorkspace { UserId = 5, WorkspaceId = 1, Accepted = false });
        var svc = new WorkspaceAccessService(uw.Object, Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserHasAccessAsync(5, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasAccessAsync_Returns_False_For_NonMember()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new WorkspaceAccessService(uw.Object, Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserHasAccessAsync(5, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task UserIsWorkspaceAdminAsync_Returns_True_For_Admin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), uwr.Object, Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserIsWorkspaceAdminAsync(5, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task UserIsWorkspaceAdminAsync_Returns_False_For_NonAdmin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(false);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), uwr.Object, Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserIsWorkspaceAdminAsync(5, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_Returns_Permissions_From_Repository()
    {
        var rolePerms = new Mock<IRolePermissionRepository>();
        
        rolePerms.Setup(r => r.GetEffectivePermissionsForUserAsync(1, 5)).ReturnsAsync(
            new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanView = true, CanCreate = true, CanEdit = true } }
            });
        
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), Mock.Of<IUserWorkspaceRoleRepository>(), rolePerms.Object);

        var result = await svc.GetUserPermissionsAsync(1, 5);

        Assert.NotEmpty(result);
        Assert.Contains("tickets", result.Keys);
        Assert.True(result["tickets"].CanView);
        Assert.True(result["tickets"].CanCreate);
    }

    [Fact]
    public async Task CanUserPerformActionAsync_Returns_True_For_Admin()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        uwr.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), uwr.Object, Mock.Of<IRolePermissionRepository>());

        var result = await svc.CanUserPerformActionAsync(1, 5, "tickets", "view");
        Assert.True(result);
    }
}
