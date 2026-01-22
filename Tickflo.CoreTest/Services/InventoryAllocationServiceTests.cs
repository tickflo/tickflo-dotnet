namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class InventoryAllocationServiceTests
{
    [Fact]
    public async Task RegisterInventoryItemAsyncThrowsWhenDuplicateSKU()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([new() { Sku = "SKU" }]);
        var locations = Mock.Of<ILocationRepository>();
        var svc = new InventoryAllocationService(repo.Object, locations);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterInventoryItemAsync(1, new InventoryRegistrationRequest { Sku = "SKU", Name = "Item", InitialQuantity = 0 }, 2));
    }

    [Fact]
    public async Task AllocateToLocationAsyncThrowsWhenLocationInactive()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.FindAsync(1, 5)).ReturnsAsync(new Inventory { Id = 5 });
        var locationRepository = new Mock<ILocationRepository>();
        locationRepository.Setup(r => r.FindAsync(1, 9)).ReturnsAsync(new Location { Id = 9, Active = false });
        var svc = new InventoryAllocationService(repo.Object, locationRepository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AllocateToLocationAsync(1, 5, 9, 3));
    }
}
