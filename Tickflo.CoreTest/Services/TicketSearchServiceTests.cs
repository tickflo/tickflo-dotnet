using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Throws_When_No_Access()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new TicketSearchService(ticketRepo.Object, uw.Object, Mock.Of<ITeamMemberRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SearchAsync(1, new TicketSearchCriteria { PageNumber = 1, PageSize = 10 }, 5));
    }

    [Fact]
    public async Task GetUnassignedTicketsAsync_Filters()
    {
        var tickets = new List<Ticket>
        {
            new() { Id = 1, AssignedUserId = null, AssignedTeamId = null, Status = "Open" },
            new() { Id = 2, AssignedUserId = 3, Status = "Open" }
        };
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(tickets);
        var svc = new TicketSearchService(ticketRepo.Object, Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamMemberRepository>());

        var result = await svc.GetUnassignedTicketsAsync(1);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
