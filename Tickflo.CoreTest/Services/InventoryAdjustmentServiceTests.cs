using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Inventory;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class InventoryAdjustmentServiceTests
{
    [Fact]
    public async Task IncreaseQuantityAsync_Increments_And_Persists()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.FindAsync(1, 3)).ReturnsAsync(new Tickflo.Core.Entities.Inventory { Id = 3, Quantity = 5 });
        var svc = new InventoryAdjustmentService(repo.Object);

        var item = await svc.IncreaseQuantityAsync(1, 3, 2, "restock", 7);

        Assert.Equal(7, item.Quantity);
        repo.Verify(r => r.UpdateAsync(It.Is<Tickflo.Core.Entities.Inventory>(i => i.Quantity == 7)), Times.Once);
    }
}
