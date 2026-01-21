namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class InventoryAdjustmentServiceTests
{
    [Fact]
    public async Task IncreaseQuantityAsyncIncrementsAndPersists()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.FindAsync(1, 3)).ReturnsAsync(new Core.Entities.Inventory { Id = 3, Quantity = 5 });
        var svc = new InventoryAdjustmentService(repo.Object);

        var item = await svc.IncreaseQuantityAsync(1, 3, 2, "restock", 7);

        Assert.Equal(7, item.Quantity);
        repo.Verify(r => r.UpdateAsync(It.Is<Core.Entities.Inventory>(i => i.Quantity == 7)), Times.Once);
    }
}
