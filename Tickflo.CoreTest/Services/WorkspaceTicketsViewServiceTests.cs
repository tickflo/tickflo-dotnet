using Moq;
using System.Threading;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceTicketsViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ReturnsViewDataWithMetadata()
    {
        var workspaceId = 1;
        var userId = 10;

        // Repos
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userRepo = new Mock<IUserRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();

        // Setup status list
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>
            {
                new TicketStatus { Name = "Open", IsClosedState = false, Color = "#00ff00" }
            });

        // Setup priority list
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>
            {
                new TicketPriority { Name = "High", Color = "red" }
            });

        // Setup type list
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>
            {
                new TicketType { Name = "Bug", Color = "orange" }
            });

        // Setup teams
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<Team>
            {
                new Team { Id = 1, Name = "DevTeam" }
            });

        // Setup contacts
        contactRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>
            {
                new Contact { Id = 1, Name = "John Doe" }
            });

        // Setup user workspaces
        userWorkspaceRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<UserWorkspace>
            {
                new UserWorkspace { UserId = 10, WorkspaceId = workspaceId, Accepted = true }
            });

        // Setup users
        userRepo.Setup(r => r.FindByIdAsync(10))
            .ReturnsAsync(new User { Id = 10, Name = "Test User" });

        // Setup locations
        locationRepo.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(new List<Location>
            {
                new Location { Id = 1, Name = "Office" }
            });

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
            .ReturnsAsync(new List<Team>());

        var service = new WorkspaceTicketsViewService(
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            teamRepo.Object,
            contactRepo.Object,
            userWorkspaceRepo.Object,
            userRepo.Object,
            locationRepo.Object,
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
    public async Task BuildAsync_WithEmptyStatuses_UsesFallbackDefaults()
    {
        var workspaceId = 1;
        var userId = 10;

        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userRepo = new Mock<IUserRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();

        // Empty statuses - should use fallback
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>());

        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());

        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());

        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<Team>());

        contactRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());

        userWorkspaceRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<UserWorkspace>());

        locationRepo.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(new List<Location>());

        // Setup admin check (not admin in this test)
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        rolePermissionRepo.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, false))
            .ReturnsAsync("all");

        teamMemberRepo.Setup(r => r.ListTeamsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new List<Team>());

        var service = new WorkspaceTicketsViewService(
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            teamRepo.Object,
            contactRepo.Object,
            userWorkspaceRepo.Object,
            userRepo.Object,
            locationRepo.Object,
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

    [Fact]
    public async Task BuildAsync_WithTeamScope_IncludesUserTeamIds()
    {
        var workspaceId = 1;
        var userId = 10;

        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userRepo = new Mock<IUserRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();

        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>());
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<Team>());
        contactRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());
        userWorkspaceRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<UserWorkspace>());
        locationRepo.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(new List<Location>());

        // Setup admin check (not admin in this test)
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        // Scope is "team", not "all"
        rolePermissionRepo.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, false))
            .ReturnsAsync("team");

        // User belongs to teams 1 and 2
        teamMemberRepo.Setup(r => r.ListTeamsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new List<Team>
            {
                new Team { Id = 1, Name = "Team A" },
                new Team { Id = 2, Name = "Team B" }
            });

        var service = new WorkspaceTicketsViewService(
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            teamRepo.Object,
            contactRepo.Object,
            userWorkspaceRepo.Object,
            userRepo.Object,
            locationRepo.Object,
            rolePermissionRepo.Object,
            teamMemberRepo.Object,
            uwrRepo.Object);

        var view = await service.BuildAsync(workspaceId, userId);

        Assert.Equal("team", view.TicketViewScope);
        Assert.Equal(new[] { 1, 2 }, view.UserTeamIds);
    }
}

