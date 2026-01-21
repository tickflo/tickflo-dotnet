namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceAccessServiceTests
{
    [Fact]
    public async Task EnsureAdminAccessAsyncThrowsWhenNotAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(false);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), userWorkspaceRoleRepository.Object, Mock.Of<IRolePermissionRepository>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.EnsureAdminAccessAsync(5, 1));
    }

    [Fact]
    public async Task EnsureAdminAccessAsyncSucceedsForAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), userWorkspaceRoleRepository.Object, Mock.Of<IRolePermissionRepository>());

        // Should not throw
        await svc.EnsureAdminAccessAsync(5, 1);
        userWorkspaceRoleRepository.Verify(r => r.IsAdminAsync(5, 1), Times.Once);
    }

    [Fact]
    public async Task UserHasAccessAsyncReturnsTrueForAcceptedMember()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync(new UserWorkspace { UserId = 5, WorkspaceId = 1, Accepted = true });
        var svc = new WorkspaceAccessService(uw.Object, Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserHasAccessAsync(5, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task UserHasAccessAsyncReturnsFalseForNonAcceptedMember()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync(new UserWorkspace { UserId = 5, WorkspaceId = 1, Accepted = false });
        var svc = new WorkspaceAccessService(uw.Object, Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserHasAccessAsync(5, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task UserHasAccessAsyncReturnsFalseForNonMember()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new WorkspaceAccessService(uw.Object, Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserHasAccessAsync(5, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task UserIsWorkspaceAdminAsyncReturnsTrueForAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), userWorkspaceRoleRepository.Object, Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserIsWorkspaceAdminAsync(5, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task UserIsWorkspaceAdminAsyncReturnsFalseForNonAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(false);
        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), userWorkspaceRoleRepository.Object, Mock.Of<IRolePermissionRepository>());

        var result = await svc.UserIsWorkspaceAdminAsync(5, 1);
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserPermissionsAsyncReturnsPermissionsFromRepository()
    {
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();

        rolePermissionRepository.Setup(r => r.GetEffectivePermissionsForUserAsync(1, 5)).ReturnsAsync(
            new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanView = true, CanCreate = true, CanEdit = true } }
            });

        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), Mock.Of<IUserWorkspaceRoleRepository>(), rolePermissionRepository.Object);

        var result = await svc.GetUserPermissionsAsync(1, 5);

        Assert.NotEmpty(result);
        Assert.Contains("tickets", result.Keys);
        Assert.True(result["tickets"].CanView);
        Assert.True(result["tickets"].CanCreate);
    }

    [Fact]
    public async Task CanUserPerformActionAsyncReturnsTrueForAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        userWorkspaceRoleRepository.Setup(r => r.IsAdminAsync(5, 1)).ReturnsAsync(true);

        var svc = new WorkspaceAccessService(Mock.Of<IUserWorkspaceRepository>(), userWorkspaceRoleRepository.Object, Mock.Of<IRolePermissionRepository>());

        var result = await svc.CanUserPerformActionAsync(1, 5, "tickets", "view");
        Assert.True(result);
    }
}
