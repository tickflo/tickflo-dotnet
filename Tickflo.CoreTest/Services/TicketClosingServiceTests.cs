using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketClosingServiceTests
{
    [Fact]
    public async Task CloseTicketAsync_Throws_When_Already_Closed()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        var statuses = new List<TicketStatus> 
        { 
            new TicketStatus { Id = 1, Name = "Closed", IsClosedState = true } 
        };
        ticketRepo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, StatusId = 1 });
        var statusRepo = new Mock<ITicketStatusRepository>();
        statusRepo.Setup(r => r.FindByNameAsync(1, "Closed")).ReturnsAsync(statuses[0]);
        var svc = new TicketClosingService(ticketRepo.Object, Mock.Of<ITicketHistoryRepository>(), statusRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CloseTicketAsync(1, 2, "note", 5));
    }

    [Fact]
    public async Task ResolveTicketAsync_Writes_History()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        var openStatus = new TicketStatus { Id = 2, Name = "Open", IsClosedState = false };
        var resolvedStatus = new TicketStatus { Id = 3, Name = "Resolved", IsClosedState = true };
        ticketRepo.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 3, StatusId = 2 });
        var history = new Mock<ITicketHistoryRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        statusRepo.Setup(r => r.FindByNameAsync(1, "Resolved")).ReturnsAsync(resolvedStatus);
        var svc = new TicketClosingService(ticketRepo.Object, history.Object, statusRepo.Object);

        var ticket = await svc.ResolveTicketAsync(1, 3, "done", 7);

        Assert.Equal(3, ticket.StatusId); // Now checking StatusId instead of Status string
        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "resolved")), Times.Once);
    }
}
