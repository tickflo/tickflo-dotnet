namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class LocationListingServiceTests
{
    [Fact]
    public async Task GetListAsyncIncludesContactCounts()
    {
        var repo = new Mock<ILocationRepository>();
        repo.Setup(r => r.ListAsync(1)).ReturnsAsync([new() { Id = 10, Name = "HQ", Address = "A" }]);
        repo.Setup(r => r.ListContactIdsAsync(1, 10)).ReturnsAsync([1, 2]);
        repo.Setup(r => r.ListContactNamesAsync(1, 10, 3)).ReturnsAsync(["A", "B"]);

        var svc = new LocationListingService(repo.Object);
        var items = await svc.GetListAsync(1);

        Assert.Single(items);
        Assert.Equal(2, items[0].ContactCount);
        Assert.Contains("A", items[0].ContactPreview);
    }
}
