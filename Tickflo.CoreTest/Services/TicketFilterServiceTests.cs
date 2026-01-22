namespace Tickflo.CoreTest.Services;

using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
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

    [Fact]
    public void ResolveStatusIdReturnsNullForOpen()
    {
        var svc = new TicketFilterService();
        var statuses = new List<TicketStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "Closed" }
        };

        var result = svc.ResolveStatusId("Open", statuses);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveStatusIdFindsMatchingStatus()
    {
        var svc = new TicketFilterService();
        var statuses = new List<TicketStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "Closed" }
        };

        var result = svc.ResolveStatusId("New", statuses);

        Assert.Equal(1, result);
    }

    [Fact]
    public void ResolvePriorityIdFindsMatchingPriority()
    {
        var svc = new TicketFilterService();
        var priorities = new List<TicketPriority>
        {
            new() { Id = 1, Name = "Low" },
            new() { Id = 2, Name = "High" }
        };

        var result = svc.ResolvePriorityId("High", priorities);

        Assert.Equal(2, result);
    }

    [Fact]
    public void ResolveTypeIdFindsMatchingType()
    {
        var svc = new TicketFilterService();
        var types = new List<TicketType>
        {
            new() { Id = 1, Name = "Bug" },
            new() { Id = 2, Name = "Feature" }
        };

        var result = svc.ResolveTypeId("Feature", types);

        Assert.Equal(2, result);
    }

    [Fact]
    public void ApplyOpenStatusFilterExcludesClosedTickets()
    {
        var svc = new TicketFilterService();
        var statuses = new List<TicketStatus>
        {
            new() { Id = 1, Name = "New", IsClosedState = false },
            new() { Id = 2, Name = "Closed", IsClosedState = true }
        };
        var tickets = new List<Ticket>
        {
            new() { Id = 1, StatusId = 1 },
            new() { Id = 2, StatusId = 2 },
            new() { Id = 3, StatusId = null }
        };

        var result = svc.ApplyOpenStatusFilter(tickets, statuses);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == 1);
        Assert.Contains(result, t => t.Id == 3);
    }

    [Fact]
    public void ApplyContactFilterMatchesNameAndEmail()
    {
        var svc = new TicketFilterService();
        var contacts = new Dictionary<int, Contact>
        {
            { 1, new() { Id = 1, Name = "John Doe", Email = "john@example.com" } },
            { 2, new() { Id = 2, Name = "Jane Smith", Email = "jane@example.com" } }
        };
        var tickets = new List<Ticket>
        {
            new() { Id = 1, ContactId = 1 },
            new() { Id = 2, ContactId = 2 },
            new() { Id = 3, ContactId = null }
        };

        var result = svc.ApplyContactFilter(tickets, "john", contacts);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyTeamFilterMatchesTeamName()
    {
        var svc = new TicketFilterService();
        var teams = new Dictionary<int, Team>
        {
            { 1, new() { Id = 1, Name = "DevTeam" } },
            { 2, new() { Id = 2, Name = "SupportTeam" } }
        };
        var tickets = new List<Ticket>
        {
            new() { Id = 1, AssignedTeamId = 1 },
            new() { Id = 2, AssignedTeamId = 2 },
            new() { Id = 3, AssignedTeamId = null }
        };

        var result = svc.ApplyTeamFilter(tickets, "DevTeam", teams);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void PaginateReturnsCorrectPage()
    {
        var svc = new TicketFilterService();
        var tickets = Enumerable.Range(1, 100).Select(i => new Ticket { Id = i }).ToList();

        var result = svc.Paginate(tickets, pageNumber: 2, pageSize: 25);

        Assert.Equal(25, result.Count);
        Assert.Equal(26, result[0].Id);
        Assert.Equal(50, result[24].Id);
    }
}
