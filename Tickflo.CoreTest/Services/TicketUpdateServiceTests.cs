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
        ticketRepo.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, StatusId = 1 });
        var statusRepo = new Mock<ITicketStatusRepository>();
        statusRepo.Setup(r => r.FindByNameAsync(1, "Closed")).ReturnsAsync((TicketStatus?)null); // Status not found
        var svc = new TicketUpdateService(ticketRepo.Object, Mock.Of<ITicketHistoryRepository>(), statusRepo.Object, Mock.Of<ITicketPriorityRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateStatusAsync(1, 2, "Closed", null, 9));
    }

    [Fact]
    public async Task UpdatePriorityAsync_Logs_History()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 3, StatusId = 1, PriorityId = 1 });
        var history = new Mock<ITicketHistoryRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        priorityRepo.Setup(r => r.FindAsync(1, "High")).ReturnsAsync(new TicketPriority { Id = 2, Name = "High" });
        var svc = new TicketUpdateService(ticketRepo.Object, history.Object, Mock.Of<ITicketStatusRepository>(), priorityRepo.Object);

        await svc.UpdatePriorityAsync(1, 3, "High", "because", 7);

        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "priority_changed")), Times.Once);
    }
}
