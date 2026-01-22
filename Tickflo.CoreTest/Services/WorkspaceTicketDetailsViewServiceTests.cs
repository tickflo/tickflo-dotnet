namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceTicketDetailsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncForNewTicketReturnsViewDataWithDefaults()
    {
        var workspaceId = 1;
        var ticketId = 0;
        var userId = 10;

        var ticketRepository = new Mock<ITicketRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var historyRepository = new Mock<ITicketHistoryRepository>();
        var userRepository = new Mock<IUserRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepository = new Mock<IInventoryRepository>();
        var locationRepository = new Mock<ILocationRepository>();
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
        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        statusRepository.Setup(r => r.FindByNameAsync(workspaceId, "New"))
            .ReturnsAsync(new TicketStatus { Id = 1, Name = "New" });
        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        priorityRepository.Setup(r => r.FindAsync(workspaceId, "Normal"))
            .ReturnsAsync(new TicketPriority { Id = 1, Name = "Normal" });
        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ticketTypeRepository.Setup(r => r.FindByNameAsync(workspaceId, "Standard"))
            .ReturnsAsync(new TicketType { Id = 1, Name = "Standard" });

        contactRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        inventoryRepository.Setup(r => r.ListAsync(workspaceId, null, "active"))
            .ReturnsAsync([]);
        userWorkspaceRepository.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        locationRepository.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync([]);

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepository.Object,
            contactRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            historyRepository.Object,
            userRepository.Object,
            userWorkspaceRepository.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.NotNull(view);
        Assert.NotNull(view.Ticket);
        Assert.NotNull(view.Ticket.TicketTypeId); // Now checking ID-based properties
        Assert.NotNull(view.Ticket.PriorityId);
        Assert.NotNull(view.Ticket.StatusId);
        Assert.True(view.CanCreateTickets);
        Assert.True(view.CanEditTickets);
        Assert.Equal(3, view.Statuses.Count);
        Assert.Equal(3, view.Priorities.Count);
        Assert.Equal(3, view.Types.Count);
    }

    [Fact]
    public async Task BuildAsyncForExistingTicketReturnsTicketAndHistory()
    {
        var workspaceId = 1;
        var ticketId = 5;
        var userId = 10;

        var ticketRepository = new Mock<ITicketRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var historyRepository = new Mock<ITicketHistoryRepository>();
        var userRepository = new Mock<IUserRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepository = new Mock<IInventoryRepository>();
        var locationRepository = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = true, CanView = true, TicketViewScope = "all" } }
            });

        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        var ticket = new Ticket { Id = ticketId, WorkspaceId = workspaceId, Subject = "Test", TicketTypeId = 1, StatusId = 1 };
        ticketRepository.Setup(r => r.FindAsync(workspaceId, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var history = new List<TicketHistory> { new() { Id = 1, TicketId = ticketId, Field = "Status", OldValue = "New", NewValue = "Open" } };
        historyRepository.Setup(r => r.ListForTicketAsync(workspaceId, ticketId))
            .ReturnsAsync(history);

        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        contactRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        inventoryRepository.Setup(r => r.ListAsync(workspaceId, null, "active"))
            .ReturnsAsync([]);
        userWorkspaceRepository.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        locationRepository.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync([]);

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepository.Object,
            contactRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            historyRepository.Object,
            userRepository.Object,
            userWorkspaceRepository.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.NotNull(view);
        Assert.Equal(ticketId, view.Ticket?.Id);
        Assert.Single(view.History);
    }

    [Fact]
    public async Task BuildAsyncWithMineScopeReturnsNullIfNotAssignedToUser()
    {
        var workspaceId = 1;
        var ticketId = 5;
        var userId = 10;
        var assignedUserId = 20;

        var ticketRepository = new Mock<ITicketRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var historyRepository = new Mock<ITicketHistoryRepository>();
        var userRepository = new Mock<IUserRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepository = new Mock<IInventoryRepository>();
        var locationRepository = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = true, CanView = true, TicketViewScope = "mine" } }
            });

        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(false);

        var ticket = new Ticket { Id = ticketId, WorkspaceId = workspaceId, Subject = "Test", AssignedUserId = assignedUserId };
        ticketRepository.Setup(r => r.FindAsync(workspaceId, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepository.Object,
            contactRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            historyRepository.Object,
            userRepository.Object,
            userWorkspaceRepository.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.Null(view);
    }

    [Fact]
    public async Task BuildAsyncAsAdminIgnoresScopeRestrictions()
    {
        var workspaceId = 1;
        var ticketId = 5;
        var userId = 10;

        var ticketRepository = new Mock<ITicketRepository>();
        var contactRepository = new Mock<IContactRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var historyRepository = new Mock<ITicketHistoryRepository>();
        var userRepository = new Mock<IUserRepository>();
        var userWorkspaceRepository = new Mock<IUserWorkspaceRepository>();
        var userWorkspaceRoleRepo = new Mock<IUserWorkspaceRoleRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var teamMemberRepo = new Mock<ITeamMemberRepository>();
        var inventoryRepository = new Mock<IInventoryRepository>();
        var locationRepository = new Mock<ILocationRepository>();
        var rolePermissionRepo = new Mock<IRolePermissionRepository>();

        // Empty permissions
        rolePermissionRepo.Setup(r => r.GetEffectivePermissionsForUserAsync(workspaceId, userId))
            .ReturnsAsync([]);

        // Admin
        userWorkspaceRoleRepo.Setup(r => r.IsAdminAsync(userId, workspaceId))
            .ReturnsAsync(true);

        var ticket = new Ticket { Id = ticketId, WorkspaceId = workspaceId, Subject = "Test", AssignedUserId = 999 };
        ticketRepository.Setup(r => r.FindAsync(workspaceId, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        contactRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        inventoryRepository.Setup(r => r.ListAsync(workspaceId, null, "active"))
            .ReturnsAsync([]);
        userWorkspaceRepository.Setup(r => r.FindForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId))
            .ReturnsAsync([]);
        locationRepository.Setup(r => r.ListAsync(workspaceId))
            .ReturnsAsync([]);
        historyRepository.Setup(r => r.ListForTicketAsync(workspaceId, ticketId))
            .ReturnsAsync([]);

        var service = new WorkspaceTicketDetailsViewService(
            ticketRepository.Object,
            contactRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            ticketTypeRepository.Object,
            historyRepository.Object,
            userRepository.Object,
            userWorkspaceRepository.Object,
            userWorkspaceRoleRepo.Object,
            teamRepo.Object,
            teamMemberRepo.Object,
            inventoryRepository.Object,
            locationRepository.Object,
            rolePermissionRepo.Object);

        var view = await service.BuildAsync(workspaceId, ticketId, userId, null);

        Assert.NotNull(view);
        Assert.True(view.IsWorkspaceAdmin);
        Assert.Equal(ticketId, view.Ticket?.Id);
    }
}

