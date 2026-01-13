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
        ticketRepo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, Status = "Closed" });
        var svc = new TicketClosingService(ticketRepo.Object, Mock.Of<ITicketHistoryRepository>(), Mock.Of<ITicketStatusRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CloseTicketAsync(1, 2, "note", 5));
    }

    [Fact]
    public async Task ResolveTicketAsync_Writes_History()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 3, Status = "Open" });
        var history = new Mock<ITicketHistoryRepository>();
        var svc = new TicketClosingService(ticketRepo.Object, history.Object, Mock.Of<ITicketStatusRepository>());

        var ticket = await svc.ResolveTicketAsync(1, 3, "done", 7);

        Assert.Equal("Resolved", ticket.Status);
        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "resolved")), Times.Once);
    }
}
