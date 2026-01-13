using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Inventory;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class InventoryAllocationServiceTests
{
    [Fact]
    public async Task RegisterInventoryItemAsync_Throws_When_Duplicate_SKU()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync(new List<Tickflo.Core.Entities.Inventory> { new() { Sku = "SKU" } });
        var locations = Mock.Of<ILocationRepository>();
        var svc = new InventoryAllocationService(repo.Object, locations);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterInventoryItemAsync(1, new InventoryRegistrationRequest { Sku = "SKU", Name = "Item", InitialQuantity = 0 }, 2));
    }

    [Fact]
    public async Task AllocateToLocationAsync_Throws_When_LocationInactive()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.FindAsync(1, 5)).ReturnsAsync(new Tickflo.Core.Entities.Inventory { Id = 5 });
        var locationRepo = new Mock<ILocationRepository>();
        locationRepo.Setup(r => r.FindAsync(1, 9)).ReturnsAsync(new Location { Id = 9, Active = false });
        var svc = new InventoryAllocationService(repo.Object, locationRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AllocateToLocationAsync(1, 5, 9, 3));
    }
}
