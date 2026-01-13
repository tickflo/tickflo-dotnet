using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketUpdateServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_Throws_On_Invalid_Transition()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, Status = "New" });
        var svc = new TicketUpdateService(ticketRepo.Object, Mock.Of<ITicketHistoryRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateStatusAsync(1, 2, "Closed", null, 9));
    }

    [Fact]
    public async Task UpdatePriorityAsync_Logs_History()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 3, Status = "Open", Priority = "Low" });
        var history = new Mock<ITicketHistoryRepository>();
        var svc = new TicketUpdateService(ticketRepo.Object, history.Object);

        await svc.UpdatePriorityAsync(1, 3, "High", "because", 7);

        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "priority_changed")), Times.Once);
    }
}
