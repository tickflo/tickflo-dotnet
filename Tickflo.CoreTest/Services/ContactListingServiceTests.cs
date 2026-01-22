namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class ContactListingServiceTests
{
    [Fact]
    public async Task GetListAsyncFiltersByPriorityAndSearch()
    {
        var contacts = new List<Contact>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "Alice", Priority = "High", Email = "a@test.com" },
            new() { Id = 2, WorkspaceId = 1, Name = "Bob", Priority = "Low", Email = "b@test.com" }
        };
        var contactRepository = new Mock<IContactRepository>();
        contactRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(contacts);
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);

        var svc = new ContactListingService(contactRepository.Object, priorityRepository.Object);
        var (items, _) = await svc.GetListAsync(1, "High", "Alice");

        Assert.Single(items);
        Assert.Equal("Alice", items[0].Name);
    }
}
