namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TicketCreationServiceTests
{
    [Fact]
    public async Task CreateTicketAsyncThrowsForInactiveLocation()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        var history = new Mock<ITicketHistoryRepository>();
        var uw = new Mock<IUserWorkspaceRepository>();
        var team = new Mock<ITeamRepository>();
        var location = new Mock<ILocationRepository>();
        location.Setup(r => r.FindAsync(1, 9)).ReturnsAsync(new Location { Id = 9, Active = false });
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var svc = new TicketCreationService(ticketRepository.Object, history.Object, uw.Object, team.Object, location.Object, statusRepository.Object, priorityRepository.Object, ticketTypeRepository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateTicketAsync(1, new TicketCreationRequest { Subject = "S", LocationId = 9 }, 3));
    }

    [Fact]
    public async Task CreateTicketAsyncSetsDefaultsAndLogsHistory()
    {
        var ticketRepository = new Mock<ITicketRepository>();
        ticketRepository.Setup(r => r.CreateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ticket t, CancellationToken _) =>
        {
            t.Id = 10;
            return t;
        });
        var history = new Mock<ITicketHistoryRepository>();
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(It.IsAny<int>(), 1)).ReturnsAsync(new UserWorkspace { WorkspaceId = 1, Accepted = true, UserId = 4 });
        var team = new Mock<ITeamRepository>();
        var location = new Mock<ILocationRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.FindByNameAsync(1, "New")).ReturnsAsync(new TicketStatus { Id = 1, Name = "New" });
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.FindAsync(1, "Normal")).ReturnsAsync(new TicketPriority { Id = 1, Name = "Normal" });
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        ticketTypeRepository.Setup(r => r.FindByNameAsync(1, "Standard")).ReturnsAsync(new TicketType { Id = 1, Name = "Standard" });
        var svc = new TicketCreationService(ticketRepository.Object, history.Object, uw.Object, team.Object, location.Object, statusRepository.Object, priorityRepository.Object, ticketTypeRepository.Object);

        var ticket = await svc.CreateTicketAsync(1, new TicketCreationRequest { Subject = "New" }, 2);

        Assert.NotNull(ticket.StatusId); // Now using ID-based fields
        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "created" && th.TicketId == 10)), Times.Once);
    }
}
