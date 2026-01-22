namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetTicketStatsAsyncComputesOpenAndResolved()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var userRepository = new Mock<IUserRepository>();
        var uwRepo = new Mock<IUserWorkspaceRepository>();
        var teamMembers = new Mock<ITeamMemberRepository>();
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();

        ticketRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new() { Id = 1, WorkspaceId = 1, StatusId = 1 },
            new() { Id = 2, WorkspaceId = 1, StatusId = 2 }
        ]);
        statusRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new() { Id = 1, Name = "Open", IsClosedState = false },
            new() { Id = 2, Name = "Closed", IsClosedState = true }
        ]);
        uwRepo.Setup(r => r.FindForWorkspaceAsync(1)).ReturnsAsync([new() { Accepted = true }]);

        var svc = new DashboardService(ticketRepository.Object, statusRepository.Object, priorityRepository.Object, userRepository.Object, uwRepo.Object, teamMembers.Object, userWorkspaceRoleRepository.Object, rolePermissionRepository.Object);
        var result = await svc.GetTicketStatsAsync(1, 7, "all", []);

        Assert.Equal(2, result.TotalTickets);
        Assert.Equal(1, result.OpenTickets);
        Assert.Equal(1, result.ResolvedTickets);
        Assert.Equal(1, result.ActiveMembers);
    }

    [Fact]
    public void FilterTicketsByAssignmentFiltersUnassigned()
    {
        var svc = new DashboardService(Mock.Of<ITicketRepository>(), Mock.Of<ITicketStatusRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamMemberRepository>(), Mock.Of<IUserWorkspaceRoleRepository>(), Mock.Of<IRolePermissionRepository>());
        var tickets = new List<Ticket>
        {
            new() { Id = 1, AssignedUserId = null },
            new() { Id = 2, AssignedUserId = 5 }
        };

        var filtered = svc.FilterTicketsByAssignment(tickets, "unassigned", 5);
        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].Id);
    }
}
