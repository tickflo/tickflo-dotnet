namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class InventoryListingServiceTests
{
    [Fact]
    public async Task GetListAsyncReturnsReadOnlyList()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([new() { Id = 1 }]);
        var svc = new InventoryListingService(repo.Object);

        var result = await svc.GetListAsync(1);

        Assert.Single(result);
        repo.Verify(r => r.ListAsync(1, null, null), Times.Once);
    }
}
