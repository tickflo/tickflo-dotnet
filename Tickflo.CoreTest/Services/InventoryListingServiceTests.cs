using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Inventory;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class InventoryListingServiceTests
{
    [Fact]
    public async Task GetListAsync_Returns_ReadOnly_List()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync(new List<Tickflo.Core.Entities.Inventory> { new() { Id = 1 } });
        var svc = new InventoryListingService(repo.Object);

        var result = await svc.GetListAsync(1);

        Assert.Single(result);
        repo.Verify(r => r.ListAsync(1, null, null), Times.Once);
    }
}
