using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Export;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ExportServiceTests
{
    [Fact]
    public async Task ValidateExportAsync_Fails_When_NoAccess()
    {
        var userWorkspaces = new Mock<IUserWorkspaceRepository>();
        userWorkspaces.Setup(r => r.FindAsync(5, 1)).ReturnsAsync((UserWorkspace?)null);
        var svc = new ExportService(Mock.Of<ITicketRepository>(), Mock.Of<IContactRepository>(), Mock.Of<IInventoryRepository>(), Mock.Of<ITicketHistoryRepository>(), userWorkspaces.Object);

        var (isValid, error) = await svc.ValidateExportAsync(1, new ExportRequest { EntityType = "tickets", Format = ExportFormat.CSV }, 5);

        Assert.False(isValid);
        Assert.Contains("access", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportTicketsAsync_Uses_Format_And_Validates_Access()
    {
        var tickets = new List<Ticket> { new() { Id = 1, Subject = "S" } };
        var ticketRepo = new Mock<ITicketRepository>();
        ticketRepo.Setup(r => r.ListAsync(2, CancellationToken.None)).ReturnsAsync(tickets);
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindAsync(9, 2)).ReturnsAsync(new UserWorkspace { Accepted = true });
        var svc = new ExportService(ticketRepo.Object, Mock.Of<IContactRepository>(), Mock.Of<IInventoryRepository>(), Mock.Of<ITicketHistoryRepository>(), uw.Object);

        var result = await svc.ExportTicketsAsync(2, new ExportRequest { EntityType = "tickets", Format = ExportFormat.CSV, Fields = new List<string> { "Id" } }, 9);

        Assert.Equal("text/csv", result.ContentType);
        Assert.Equal(1, result.RecordCount);
    }
}
