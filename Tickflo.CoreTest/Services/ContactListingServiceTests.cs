using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Contacts;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ContactListingServiceTests
{
    [Fact]
    public async Task GetListAsync_Filters_ByPriority_And_Search()
    {
        var contacts = new List<Contact>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "Alice", Priority = "High", Email = "a@test.com" },
            new() { Id = 2, WorkspaceId = 1, Name = "Bob", Priority = "Low", Email = "b@test.com" }
        };
        var contactRepo = new Mock<IContactRepository>();
        contactRepo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(contacts);
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        priorityRepo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<TicketPriority>());

        var svc = new ContactListingService(contactRepo.Object, priorityRepo.Object);
        var (items, _) = await svc.GetListAsync(1, "High", "Alice");

        Assert.Single(items);
        Assert.Equal("Alice", items[0].Name);
    }
}
