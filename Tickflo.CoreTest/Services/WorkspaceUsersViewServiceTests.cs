using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceUsersViewServiceTests
{
    [Fact]
    public async Task BuildAsync_LoadsPermissionsAndPendingInvites()
    {
        var workspaceId = 1;
        var userId = 10;

        var uwRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var rolePermRepo = new Mock<IRolePermissionRepository>();
        var uwRepo = new Mock<IUserWorkspaceRepository>();
        var userRepo = new Mock<IUserRepository>();

        uwRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId)).ReturnsAsync(false);
        rolePermRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "users", new EffectiveSectionPermission { Section = "users", CanCreate = true, CanEdit = true, CanView = true } }
            });

        uwRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId)).ReturnsAsync(new List<UserWorkspace>
        {
            new UserWorkspace { UserId = 20, WorkspaceId = workspaceId, Accepted = false, CreatedAt = new DateTime(2024,1,1) }
        });

        userRepo.Setup(r => r.FindByIdAsync(20)).ReturnsAsync(new User { Id = 20, Email = "a@test.com" });
        uwRoleRepo.Setup(r => r.GetRoleNamesAsync(20, workspaceId)).ReturnsAsync(new List<string> { "member" });

        var service = new WorkspaceUsersViewService(
            uwRoleRepo.Object,
            rolePermRepo.Object,
            uwRepo.Object,
            userRepo.Object);

        var view = await service.BuildAsync(workspaceId, userId);

        Assert.False(view.IsWorkspaceAdmin);
        Assert.True(view.CanCreateUsers);
        Assert.True(view.CanEditUsers);
        Assert.Single(view.PendingInvites);
        Assert.Equal(20, view.PendingInvites[0].UserId);
        Assert.Equal("a@test.com", view.PendingInvites[0].Email);
        Assert.Equal("member", view.PendingInvites[0].Roles.Single());
    }

    [Fact]
    public async Task BuildAsync_AdminOverridesPermissions()
    {
        var workspaceId = 1;
        var userId = 10;

        var uwRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var rolePermRepo = new Mock<IRolePermissionRepository>();
        var uwRepo = new Mock<IUserWorkspaceRepository>();
        var userRepo = new Mock<IUserRepository>();

        uwRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId)).ReturnsAsync(true);
        rolePermRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        uwRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId)).ReturnsAsync(new List<UserWorkspace>());

        var service = new WorkspaceUsersViewService(
            uwRoleRepo.Object,
            rolePermRepo.Object,
            uwRepo.Object,
            userRepo.Object);

        var view = await service.BuildAsync(workspaceId, userId);

        Assert.True(view.IsWorkspaceAdmin);
        Assert.True(view.CanCreateUsers);
        Assert.True(view.CanEditUsers);
    }
}

