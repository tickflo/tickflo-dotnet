namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TicketUpdateServiceTests
{
    [Fact]
    public async Task UpdateStatusAsyncThrowsOnInvalidTransition()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 2, StatusId = 1 });
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.FindByNameAsync(1, "Closed")).ReturnsAsync((TicketStatus?)null); // Status not found
        var svc = new TicketUpdateService(ticketRepository.Object, Mock.Of<ITicketHistoryRepository>(), statusRepository.Object, Mock.Of<ITicketPriorityRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateStatusAsync(1, 2, "Closed", null, 9));
    }

    [Fact]
    public async Task UpdatePriorityAsyncLogsHistory()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 3, StatusId = 1, PriorityId = 1 });
        var history = new Mock<ITicketHistoryRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.FindAsync(1, "High")).ReturnsAsync(new TicketPriority { Id = 2, Name = "High" });
        var svc = new TicketUpdateService(ticketRepository.Object, history.Object, Mock.Of<ITicketStatusRepository>(), priorityRepository.Object);

        await svc.UpdatePriorityAsync(1, 3, "High", "because", 7);

        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "priority_changed")), Times.Once);
    }

    [Fact]
    public async Task UpdatePriorityAsyncThrowsWhenPriorityNotFound()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(new Ticket { Id = 3, StatusId = 1, PriorityId = 1 });
        var history = new Mock<ITicketHistoryRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.FindAsync(1, "InvalidPriority")).ReturnsAsync((TicketPriority?)null);
        var svc = new TicketUpdateService(ticketRepository.Object, history.Object, Mock.Of<ITicketStatusRepository>(), priorityRepository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdatePriorityAsync(1, 3, "InvalidPriority", "test", 7));
    }

    [Fact]
    public async Task UpdateStatusAsyncSucceedsWhenStatusFound()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var ticket = new Ticket { Id = 2, StatusId = 1 };
        ticketRepository.Setup(r => r.FindAsync(1, 2, CancellationToken.None)).ReturnsAsync(ticket);
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.FindByNameAsync(1, "Resolved")).ReturnsAsync(new TicketStatus { Id = 3, Name = "Resolved", IsClosedState = true });
        var history = new Mock<ITicketHistoryRepository>();
        var svc = new TicketUpdateService(ticketRepository.Object, history.Object, statusRepository.Object, Mock.Of<ITicketPriorityRepository>());

        var result = await svc.UpdateStatusAsync(1, 2, "Resolved", "fixed", 7);

        Assert.Equal(3, result.StatusId);
        history.Verify(h => h.CreateAsync(It.IsAny<TicketHistory>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePriorityAsyncUpdatesTicketPriority()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var ticket = new Ticket { Id = 3, StatusId = 1, PriorityId = 1 };
        ticketRepository.Setup(r => r.FindAsync(1, 3, CancellationToken.None)).ReturnsAsync(ticket);
        var history = new Mock<ITicketHistoryRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.FindAsync(1, "High")).ReturnsAsync(new TicketPriority { Id = 2, Name = "High" });
        var svc = new TicketUpdateService(ticketRepository.Object, history.Object, Mock.Of<ITicketStatusRepository>(), priorityRepository.Object);

        var result = await svc.UpdatePriorityAsync(1, 3, "High", "important", 7);

        Assert.Equal(2, result.PriorityId);
    }
}
