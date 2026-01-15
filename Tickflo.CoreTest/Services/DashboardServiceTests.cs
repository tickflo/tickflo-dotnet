using Moq;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Export;
using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Services.Locations;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Storage;
using Tickflo.Core.Services.Teams;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetTicketStatsAsync_Computes_Open_And_Resolved()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var userRepo = new Mock<IUserRepository>();
        var uwRepo = new Mock<IUserWorkspaceRepository>();
        var teamMembers = new Mock<ITeamMemberRepository>();
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var rolePerms = new Mock<IRolePermissionRepository>();

        ticketRepo.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Ticket>
        {
            new Ticket { Id = 1, WorkspaceId = 1, StatusId = 1 },
            new Ticket { Id = 2, WorkspaceId = 1, StatusId = 2 }
        });
        statusRepo.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TicketStatus>
        {
            new TicketStatus { Id = 1, Name = "Open", IsClosedState = false },
            new TicketStatus { Id = 2, Name = "Closed", IsClosedState = true }
        });
        uwRepo.Setup(r => r.FindForWorkspaceAsync(1)).ReturnsAsync(new List<UserWorkspace> { new() { Accepted = true } });

        var svc = new DashboardService(ticketRepo.Object, statusRepo.Object, priorityRepo.Object, userRepo.Object, uwRepo.Object, teamMembers.Object, uwr.Object, rolePerms.Object);
        var result = await svc.GetTicketStatsAsync(1, 7, "all", new());

        Assert.Equal(2, result.TotalTickets);
        Assert.Equal(1, result.OpenTickets);
        Assert.Equal(1, result.ResolvedTickets);
        Assert.Equal(1, result.ActiveMembers);
    }

    [Fact]
    public void FilterTicketsByAssignment_Filters_Unassigned()
    {
        var svc = new DashboardService(Mock.Of<ITicketRepository>(), Mock.Of<ITicketStatusRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamMemberRepository>(), Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());
        var tickets = new List<Ticket>
        {
            new Ticket { Id = 1, AssignedUserId = null },
            new Ticket { Id = 2, AssignedUserId = 5 }
        };

        var filtered = svc.FilterTicketsByAssignment(tickets, "unassigned", 5);
        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].Id);
    }
}
