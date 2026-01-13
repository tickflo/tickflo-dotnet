using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Locations;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class LocationListingServiceTests
{
    [Fact]
    public async Task GetListAsync_Includes_ContactCounts()
    {
        var repo = new Mock<ILocationRepository>();
        repo.Setup(r => r.ListAsync(1)).ReturnsAsync(new List<Location> { new() { Id = 10, Name = "HQ", Address = "A" } });
        repo.Setup(r => r.ListContactIdsAsync(1, 10)).ReturnsAsync(new List<int> { 1, 2 });
        repo.Setup(r => r.ListContactNamesAsync(1, 10, 3)).ReturnsAsync(new List<string> { "A", "B" });

        var svc = new LocationListingService(repo.Object);
        var items = await svc.GetListAsync(1);

        Assert.Single(items);
        Assert.Equal(2, items[0].ContactCount);
        Assert.Contains("A", items[0].ContactPreview);
    }
}
