namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TicketSearchServiceTests
{
    [Fact]
    public async Task SearchAsyncThrowsWhenNoAccess()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new TicketSearchService(ticketRepository.Object, uw.Object, Mock.Of<ITeamMemberRepository>(), Mock.Of<ITicketStatusRepository>(), Mock.Of<ITicketPriorityRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SearchAsync(1, new TicketSearchCriteria { PageNumber = 1, PageSize = 10 }, 5));
    }

    [Fact]
    public async Task GetUnassignedTicketsAsyncFilters()
    {
        var tickets = new List<Ticket>
        {
            new() { Id = 1, AssignedUserId = null, AssignedTeamId = null, StatusId = 1 },
            new() { Id = 2, AssignedUserId = 3, StatusId = 1 }
        };
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(tickets);
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(
        [
            new() { Id = 1, Name = "Open", IsClosedState = false }
        ]);
        var svc = new TicketSearchService(ticketRepository.Object, Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamMemberRepository>(), statusRepository.Object, Mock.Of<ITicketPriorityRepository>());

        var result = await svc.GetUnassignedTicketsAsync(1);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
