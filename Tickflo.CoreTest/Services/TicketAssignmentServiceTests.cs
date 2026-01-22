namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TicketAssignmentServiceTests
{
    [Fact]
    public async Task AssignToUserAsyncThrowsWhenUserNotInWorkspace()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2 });
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new TicketAssignmentService(ticketRepository.Object, Mock.Of<ITicketHistoryRepository>(), uw.Object, Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AssignToUserAsync(1, 2, 5, 9));
    }

    [Fact]
    public async Task UnassignUserAsyncWritesHistory()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, AssignedUserId = 7 });
        var history = new Mock<ITicketHistoryRepository>();
        var svc = new TicketAssignmentService(ticketRepository.Object, history.Object, Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>());

        await svc.UnassignUserAsync(1, 2, 9);

        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "unassigned")), Times.Once);
    }

    [Fact]
    public async Task UpdateAssignmentAsyncReturnsFalseWhenAssignmentUnchanged()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var ticket = new Ticket { Id = 2, AssignedUserId = 5 };
        var svc = new TicketAssignmentService(ticketRepository.Object, Mock.Of<ITicketHistoryRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>());

        var result = await svc.UpdateAssignmentAsync(ticket, 5, 9);

        Assert.False(result);
        ticketRepository.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAssignmentAsyncReturnsTrueWhenAssignmentChanged()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var ticket = new Ticket { Id = 2, AssignedUserId = 5 };
        var svc = new TicketAssignmentService(ticketRepository.Object, Mock.Of<ITicketHistoryRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>());

        var result = await svc.UpdateAssignmentAsync(ticket, 7, 9);

        Assert.True(result);
        Assert.Equal(7, ticket.AssignedUserId);
        ticketRepository.Verify(r => r.UpdateAsync(ticket), Times.Once);
    }
}
