using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketCreationServiceTests
{
    [Fact]
    public async Task CreateTicketAsync_Throws_For_Inactive_Location()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        var history = new Mock<ITicketHistoryRepository>();
        var uw = new Mock<IUserWorkspaceRepository>();
        var team = new Mock<ITeamRepository>();
        var location = new Mock<ILocationRepository>();
        location.Setup(r => r.FindAsync(1, 9)).ReturnsAsync(new Location { Id = 9, Active = false });
        var svc = new TicketCreationService(ticketRepo.Object, history.Object, uw.Object, team.Object, location.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateTicketAsync(1, new TicketCreationRequest { Subject = "S", LocationId = 9 }, 3));
    }

    [Fact]
    public async Task CreateTicketAsync_Sets_Defaults_And_Logs_History()
    {
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ticket t, CancellationToken _) =>
        {
            t.Id = 10;
            return t;
        });
        var history = new Mock<ITicketHistoryRepository>();
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(It.IsAny<int>(), 1)).ReturnsAsync(new UserWorkspace { WorkspaceId = 1, Accepted = true, UserId = 4 });
        var team = new Mock<ITeamRepository>();
        var location = new Mock<ILocationRepository>();
        var svc = new TicketCreationService(ticketRepo.Object, history.Object, uw.Object, team.Object, location.Object);

        var ticket = await svc.CreateTicketAsync(1, new TicketCreationRequest { Subject = "New" }, 2);

        Assert.Equal("New", ticket.Status);
        history.Verify(h => h.CreateAsync(It.Is<TicketHistory>(th => th.Action == "created" && th.TicketId == 10)), Times.Once);
    }
}
