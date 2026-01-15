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
            new() { Id = 1, Subject = "Alpha", StatusId = 1 },
            new() { Id = 2, Subject = "Beta", StatusId = 2 }
        };

        var result = svc.ApplyFilters(tickets, new TicketFilterCriteria { StatusId = 1, Query = "Alpha" });

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }
}
