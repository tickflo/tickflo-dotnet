using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketFilterServiceTests
{
    [Fact]
    public void ApplyFilters_Filters_By_Status_And_Query()
    {
        var svc = new TicketFilterService();
        var tickets = new List<Ticket>
        {
            new() { Id = 1, Subject = "Alpha", Status = "Open" },
            new() { Id = 2, Subject = "Beta", Status = "Closed" }
        };

        var result = svc.ApplyFilters(tickets, new TicketFilterCriteria { Status = "Open", Query = "Alpha" });

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
