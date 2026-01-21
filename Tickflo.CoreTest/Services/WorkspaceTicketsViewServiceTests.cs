namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceTicketsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncReturnsViewDataWithMetadata()
    {
        var workspaceId = 1;
        var userId = 10;

        // Repos
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userRepository = new Mock<IUserRepository>();
        var locationRepository = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();

        // Setup status list
        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Name = "Open", IsClosedState = false, Color = "#00ff00" }
            ]);

        // Setup priority list
        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Name = "High", Color = "red" }
            ]);

        // Setup type list
        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Name = "Bug", Color = "orange" }
            ]);

        // Setup teams
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(
            [
                new Team { Id = 1, Name = "DevTeam" }
            ]);

        // Setup contacts
        contactRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = 1, Name = "John Doe" }
            ]);

        // Setup user workspaces
        userWorkspaceRepository.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(
            [
                new UserWorkspace { UserId = 10, WorkspaceId = workspaceId, Accepted = true }
            ]);

        // Setup users
        userRepository.Setup(r => r.FindByIdAsync(10))
            .ReturnsAsync(new User { Id = 10, Name = "Test User" });

        // Setup locations
        locationRepository.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(
            [
                new() { Id = 1, Name = "Office" }
            ]);

        // Setup permissions
        // Setup admin check (not admin in this test)
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = true, CanView = true } }
            });

        rolePermissionRepo.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, false))
            .ReturnsAsync("all");

        // Setup team members (empty for scope "all")
        teamMemberRepo.Setup(r => r.ListTeamsForUserAsync(workspaceId, userId))
            .ReturnsAsync([]);

        var service = new WorkspaceTicketsViewService(
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            teamRepo.Object,
            contactRepository.Object,
            userWorkspaceRepository.Object,
            userRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object,
            teamMemberRepo.Object,
            uwrRepo.Object);

        var view = await service.BuildAsync(workspaceId, userId);

        Assert.Single(view.Statuses);
        Assert.Equal("Open", view.Statuses[0].Name);
        Assert.Single(view.Priorities);
        Assert.Single(view.Types);
        Assert.Single(view.TeamsById);
        Assert.Single(view.ContactsById);
        Assert.Single(view.UsersById);
        Assert.Single(view.LocationOptions);
        Assert.True(view.CanCreateTickets);
        Assert.True(view.CanEditTickets);
        Assert.Equal("all", view.TicketViewScope);
        Assert.Empty(view.UserTeamIds);
    }

    [Fact]
    public async Task BuildAsyncWithEmptyStatusesUsesFallbackDefaults()
    {
        var workspaceId = 1;
        var userId = 10;

        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userRepository = new Mock<IUserRepository>();
        var locationRepository = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();

        // Empty statuses - should use fallback
        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);

        contactRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        userWorkspaceRepository.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);

        locationRepository.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync([]);

        // Setup admin check (not admin in this test)
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync([]);

        rolePermissionRepo.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, false))
            .ReturnsAsync("all");

        teamMemberRepo.Setup(r => r.ListTeamsForUserAsync(workspaceId, userId))
            .ReturnsAsync([]);

        var service = new WorkspaceTicketsViewService(
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            teamRepo.Object,
            contactRepository.Object,
            userWorkspaceRepository.Object,
            userRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object,
            teamMemberRepo.Object,
            uwrRepo.Object);

        var view = await service.BuildAsync(workspaceId, userId);

        // Should have fallback statuses
        Assert.Equal(3, view.Statuses.Count);
        Assert.Contains(view.Statuses, s => s.Name == "New");
        Assert.Contains(view.Statuses, s => s.Name == "Completed");
        Assert.Contains(view.Statuses, s => s.Name == "Closed");

        // Should have fallback priorities
        Assert.Equal(3, view.Priorities.Count);
        Assert.Contains(view.Priorities, p => p.Name == "Low");

        // Should have fallback types
        Assert.Equal(3, view.Types.Count);
        Assert.Contains(view.Types, t => t.Name == "Standard");
    }

    private static readonly int[] expected = [1, 2];

    [Fact]
    public async Task BuildAsyncWithTeamScopeIncludesUserTeamIds()
    {
        var workspaceId = 1;
        var userId = 10;

        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userRepository = new Mock<IUserRepository>();
        var locationRepository = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();

        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        contactRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        userWorkspaceRepository.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        locationRepository.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync([]);

        // Setup admin check (not admin in this test)
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync([]);

        // Scope is "team", not "all"
        rolePermissionRepo.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, false))
            .ReturnsAsync("team");

        // User belongs to teams 1 and 2
        teamMemberRepo.Setup(r => r.ListTeamsForUserAsync(workspaceId, userId))
            .ReturnsAsync(
            [
                new Team { Id = 1, Name = "Team A" },
                new Team { Id = 2, Name = "Team B" }
            ]);

        var service = new WorkspaceTicketsViewService(
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            teamRepo.Object,
            contactRepository.Object,
            userWorkspaceRepository.Object,
            userRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object,
            teamMemberRepo.Object,
            uwrRepo.Object);

        var view = await service.BuildAsync(workspaceId, userId);

        Assert.Equal("team", view.TicketViewScope);
        Assert.Equal(expected, view.UserTeamIds);
    }
}

