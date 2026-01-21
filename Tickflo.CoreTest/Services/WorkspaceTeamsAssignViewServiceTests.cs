namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceTeamsAssignViewServiceTests
{
    [Fact]
    public async Task BuildAsyncReturnsUsersAndMembersWhenAdmin()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var teams = new Mock<ITeamRepository>();
        var membersRepo = new Mock<ITeamMemberRepository>();
        var userWorkspaces = new Mock<IUserWorkspaceRepository>();
        var users = new Mock<IUserRepository>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);

        var team = new Team { Id = 3, WorkspaceId = 10, Name = "Ops" };
        teams.Setup(x => x.FindByIdAsync(3)).ReturnsAsync(team);

        membersRepo.Setup(x => x.ListMembersAsync(3))
            .ReturnsAsync([new User { Id = 2, Name = "Alice" }]);

        userWorkspaces.Setup(x => x.FindForWorkspaceAsync(10))
            .ReturnsAsync(
            [
                new UserWorkspace { UserId = 2, WorkspaceId = 10, Accepted = true },
                new UserWorkspace { UserId = 4, WorkspaceId = 10, Accepted = true }
            ]);

        users.Setup(x => x.FindByIdAsync(2)).ReturnsAsync(new User { Id = 2, Name = "Alice" });
        users.Setup(x => x.FindByIdAsync(4)).ReturnsAsync(new User { Id = 4, Name = "Bob" });

        var svc = new WorkspaceTeamsAssignViewService(userWorkspaceRoleRepository.Object, perms.Object, teams.Object, membersRepo.Object, userWorkspaces.Object, users.Object);
        var result = await svc.BuildAsync(10, 1, 3);

        Assert.True(result.CanViewTeams);
        Assert.True(result.CanEditTeams);
        Assert.NotNull(result.Team);
        Assert.Equal(2, result.WorkspaceUsers.Count);
        Assert.Single(result.Members);
        Assert.Equal(2, result.Members[0].Id);
    }

    [Fact]
    public async Task BuildAsyncDeniesWhenUserCannotView()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var teams = new Mock<ITeamRepository>();
        var membersRepo = new Mock<ITeamMemberRepository>();
        var userWorkspaces = new Mock<IUserWorkspaceRepository>();
        var users = new Mock<IUserRepository>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync([]);

        var svc = new WorkspaceTeamsAssignViewService(userWorkspaceRoleRepository.Object, perms.Object, teams.Object, membersRepo.Object, userWorkspaces.Object, users.Object);
        var result = await svc.BuildAsync(10, 2, 3);

        Assert.False(result.CanViewTeams);
        Assert.False(result.CanEditTeams);
        Assert.Null(result.Team);
        Assert.Empty(result.WorkspaceUsers);
        Assert.Empty(result.Members);
    }

    [Fact]
    public async Task BuildAsyncEmptyWhenTeamNotInWorkspace()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var teams = new Mock<ITeamRepository>();
        var membersRepo = new Mock<ITeamMemberRepository>();
        var userWorkspaces = new Mock<IUserWorkspaceRepository>();
        var users = new Mock<IUserRepository>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(3, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 3))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "teams", new EffectiveSectionPermission { Section = "teams", CanView = true, CanEdit = false } }
            });

        var team = new Team { Id = 5, WorkspaceId = 11, Name = "Other" };
        teams.Setup(x => x.FindByIdAsync(5)).ReturnsAsync(team);

        var svc = new WorkspaceTeamsAssignViewService(userWorkspaceRoleRepository.Object, perms.Object, teams.Object, membersRepo.Object, userWorkspaces.Object, users.Object);
        var result = await svc.BuildAsync(10, 3, 5);

        Assert.True(result.CanViewTeams);
        Assert.False(result.CanEditTeams);
        Assert.NotNull(result.Team);
        Assert.NotEqual(10, result.Team!.WorkspaceId);
        Assert.Empty(result.WorkspaceUsers);
        Assert.Empty(result.Members);
    }
}

