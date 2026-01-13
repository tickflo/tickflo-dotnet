using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketAssignmentServiceTests
{
    [Fact]
    public async Task AssignToUserAsync_Throws_When_User_Not_In_Workspace()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2 });
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new TicketAssignmentService(ticketRepo.Object, Mock.Of<ITicketHistoryRepository>(), uw.Object, Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AssignToUserAsync(1, 2, 5, 9));
    }

    [Fact]
    public async Task UnassignUserAsync_Writes_History()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, AssignedUserId = 7 });
        var history = new Mock<ITicketHistoryRepository>();
        var svc = new TicketAssignmentService(ticketRepo.Object, history.Object, Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>());

        await svc.UnassignUserAsync(1, 2, 9);

        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "unassigned")), Times.Once);
    }
}
