using Moq;
using System.Threading;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceTicketDetailsViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ForNewTicket_ReturnsViewDataWithDefaults()
    {
        var workspaceId = 1;
        var ticketId = 0;
        var userId = 10;

        var ticketRepo = new Mock<ITicketRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var historyRepo = new Mock<ITicketHistoryRepository>();
        var userRepo = new Mock<IUserRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepo = new Mock<IInventoryRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        // Setup permissions
        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = true, CanView = true, TicketViewScope = "all" } }
            });

        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        // Setup repositories
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>());
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());

        contactRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());
        inventoryRepo.Setup(r => r.ListAsync(workspaceId, null, "active"))
            .ReturnsAsync(new List<Inventory>());
        userWorkspaceRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<UserWorkspace>());
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<Team>());
        locationRepo.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(new List<Location>());

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepo.Object,
            contactRepo.Object,
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            historyRepo.Object,
            userRepo.Object,
            userWorkspaceRepo.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepo.Object,
            locationRepo.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.NotNull(view);
        Assert.NotNull(view.Ticket);
        Assert.Equal("Standard", view.Ticket.Type);
        Assert.Equal("Normal", view.Ticket.Priority);
        Assert.Equal("New", view.Ticket.Status);
        Assert.True(view.CanCreateTickets);
        Assert.True(view.CanEditTickets);
        Assert.Equal(3, view.Statuses.Count); // Fallback defaults
        Assert.Equal(3, view.Priorities.Count);
        Assert.Equal(3, view.Types.Count);
    }

    [Fact]
    public async Task BuildAsync_ForExistingTicket_ReturnsTicketAndHistory()
    {
        var workspaceId = 1;
        var ticketId = 5;
        var userId = 10;

        var ticketRepo = new Mock<ITicketRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var historyRepo = new Mock<ITicketHistoryRepository>();
        var userRepo = new Mock<IUserRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepo = new Mock<IInventoryRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = true, CanView = true, TicketViewScope = "all" } }
            });

        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        var ticket = new Ticket { Id = ticketId, WorkspaceId = workspaceId, Subject = "Test", Type = "Bug", Status = "Open" };
        ticketRepo.Setup(r => r.FindAsync(workspaceId, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var history = new List<TicketHistory> { new TicketHistory { Id = 1, TicketId = ticketId, Field = "Status", OldValue = "New", NewValue = "Open" } };
        historyRepo.Setup(r => r.ListForTicketAsync(workspaceId, ticketId))
            .ReturnsAsync(history);

        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>());
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());

        contactRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());
        inventoryRepo.Setup(r => r.ListAsync(workspaceId, null, "active"))
            .ReturnsAsync(new List<Inventory>());
        userWorkspaceRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<UserWorkspace>());
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<Team>());
        locationRepo.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(new List<Location>());

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepo.Object,
            contactRepo.Object,
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            historyRepo.Object,
            userRepo.Object,
            userWorkspaceRepo.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepo.Object,
            locationRepo.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.NotNull(view);
        Assert.Equal(ticketId, view.Ticket?.Id);
        Assert.Single(view.History);
    }

    [Fact]
    public async Task BuildAsync_WithMineScope_ReturnsNullIfNotAssignedToUser()
    {
        var workspaceId = 1;
        var ticketId = 5;
        var userId = 10;
        var assignedUserId = 20;

        var ticketRepo = new Mock<ITicketRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var historyRepo = new Mock<ITicketHistoryRepository>();
        var userRepo = new Mock<IUserRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepo = new Mock<IInventoryRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = true, CanView = true, TicketViewScope = "mine" } }
            });

        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        var ticket = new Ticket { Id = ticketId, WorkspaceId = workspaceId, Subject = "Test", AssignedUserId = assignedUserId };
        ticketRepo.Setup(r => r.FindAsync(workspaceId, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepo.Object,
            contactRepo.Object,
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            historyRepo.Object,
            userRepo.Object,
            userWorkspaceRepo.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepo.Object,
            locationRepo.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.Null(view);
    }

    [Fact]
    public async Task BuildAsync_AsAdmin_IgnoresScopeRestrictions()
    {
        var workspaceId = 1;
        var ticketId = 5;
        var userId = 10;

        var ticketRepo = new Mock<ITicketRepository>();
        var contactRepo = new Mock<IContactRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var historyRepo = new Mock<ITicketHistoryRepository>();
        var userRepo = new Mock<IUserRepository>();
        var userWorkspaceRepo = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepo = new Mock<IInventoryRepository>();
        var locationRepo = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        // Empty permissions
        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        // Admin
        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(true);

        var ticket = new Ticket { Id = ticketId, WorkspaceId = workspaceId, Subject = "Test", AssignedUserId = 999 };
        ticketRepo.Setup(r => r.FindAsync(workspaceId, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>());
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());

        contactRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Contact>());
        inventoryRepo.Setup(r => r.ListAsync(workspaceId, null, "active"))
            .ReturnsAsync(new List<Inventory>());
        userWorkspaceRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<UserWorkspace>());
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync(new List<Team>());
        locationRepo.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync(new List<Location>());
        historyRepo.Setup(r => r.ListForTicketAsync(workspaceId, ticketId))
            .ReturnsAsync(new List<TicketHistory>());

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepo.Object,
            contactRepo.Object,
            statusRepo.Object,
            priorityRepo.Object,
            typeRepo.Object,
            historyRepo.Object,
            userRepo.Object,
            userWorkspaceRepo.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepo.Object,
            locationRepo.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.NotNull(view);
        Assert.True(view.IsWorkspaceAdmin);
        Assert.Equal(ticketId, view.Ticket?.Id);
    }
}
