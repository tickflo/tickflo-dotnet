namespace Tickflo.CoreTest.Services;

using Tickflo.Core.Entities;
using Xunit;

public class TicketFilterServiceTests
{
    [Fact]
    public void ApplyFiltersFiltersByStatusAndQuery()
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
