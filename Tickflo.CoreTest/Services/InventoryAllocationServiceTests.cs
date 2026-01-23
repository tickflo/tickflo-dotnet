namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Inventory;
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
    public async Task RegisterInventoryItemAsyncSetsStatusWhenProvided()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([]);
        repo.Setup(r => r.CreateAsync(It.IsAny<Inventory>())).ReturnsAsync((Inventory i) => i);
        var locations = Mock.Of<ILocationRepository>();
        var svc = new InventoryAllocationService(repo.Object, locations);

        var result = await svc.RegisterInventoryItemAsync(1, new InventoryRegistrationRequest
        {
            Sku = "TEST-SKU",
            Name = "Test Item",
            InitialQuantity = 10,
            Status = "inactive"
        }, 2);

        Assert.Equal("inactive", result.Status);
    }

    [Fact]
    public async Task RegisterInventoryItemAsyncDefaultsToActiveStatusWhenNotProvided()
    {
        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([]);
        repo.Setup(r => r.CreateAsync(It.IsAny<Inventory>())).ReturnsAsync((Inventory i) => i);
        var locations = Mock.Of<ILocationRepository>();
        var svc = new InventoryAllocationService(repo.Object, locations);

        var result = await svc.RegisterInventoryItemAsync(1, new InventoryRegistrationRequest
        {
            Sku = "TEST-SKU",
            Name = "Test Item",
            InitialQuantity = 10
        }, 2);

        Assert.Equal("active", result.Status);
    }

    [Fact]
    public async Task UpdateInventoryDetailsAsyncUpdatesQuantityWhenProvided()
    {
        var repo = new Mock<IInventoryRepository>();
        var existingItem = new Inventory { Id = 5, Sku = "TEST", Name = "Test", Quantity = 10, Status = "active" };
        repo.Setup(r => r.FindAsync(1, 5)).ReturnsAsync(existingItem);
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([existingItem]);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Inventory>())).Returns(Task.CompletedTask);
        var locations = Mock.Of<ILocationRepository>();
        var svc = new InventoryAllocationService(repo.Object, locations);

        var result = await svc.UpdateInventoryDetailsAsync(1, 5, new InventoryDetailsUpdateRequest
        {
            Quantity = 25
        }, 2);

        Assert.Equal(25, result.Quantity);
    }

    [Fact]
    public async Task UpdateInventoryDetailsAsyncUpdatesStatusWhenProvided()
    {
        var repo = new Mock<IInventoryRepository>();
        var existingItem = new Inventory { Id = 5, Sku = "TEST", Name = "Test", Quantity = 10, Status = "active" };
        repo.Setup(r => r.FindAsync(1, 5)).ReturnsAsync(existingItem);
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([existingItem]);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Inventory>())).Returns(Task.CompletedTask);
        var locations = Mock.Of<ILocationRepository>();
        var svc = new InventoryAllocationService(repo.Object, locations);

        var result = await svc.UpdateInventoryDetailsAsync(1, 5, new InventoryDetailsUpdateRequest
        {
            Status = "discontinued"
        }, 2);

        Assert.Equal("discontinued", result.Status);
    }

    [Fact]
    public async Task UpdateInventoryDetailsAsyncUpdatesLocationIdWhenProvided()
    {
        var repo = new Mock<IInventoryRepository>();
        var existingItem = new Inventory { Id = 5, Sku = "TEST", Name = "Test", Quantity = 10, Status = "active", LocationId = null };
        repo.Setup(r => r.FindAsync(1, 5)).ReturnsAsync(existingItem);
        repo.Setup(r => r.ListAsync(1, null, null)).ReturnsAsync([existingItem]);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Inventory>())).Returns(Task.CompletedTask);
        var locationRepo = new Mock<ILocationRepository>();
        locationRepo.Setup(r => r.FindAsync(1, 3)).ReturnsAsync(new Location { Id = 3, Active = true });
        var svc = new InventoryAllocationService(repo.Object, locationRepo.Object);

        var result = await svc.UpdateInventoryDetailsAsync(1, 5, new InventoryDetailsUpdateRequest
        {
            LocationId = 3
        }, 2);

        Assert.Equal(3, result.LocationId);
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
