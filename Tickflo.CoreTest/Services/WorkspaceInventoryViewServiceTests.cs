using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceInventoryViewServiceTests
{
    [Fact]
    public async Task BuildAsync_LoadsInventoryWithPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<IInventoryListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "inventory", new EffectiveSectionPermission { Section = "inventory", CanCreate = true, CanEdit = true } }
        };

        var inventoryItems = new List<Inventory>
        {
            new Inventory { Id = 1, WorkspaceId = 1, Name = "Widget A", Status = "active" },
            new Inventory { Id = 2, WorkspaceId = 1, Name = "Widget B", Status = "active" }
        };

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync(inventoryItems);

        var service = new WorkspaceInventoryViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.IsWorkspaceAdmin);
        Assert.True(result.CanCreateInventory);
        Assert.True(result.CanEditInventory);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("Widget A", result.Items.First().Name);
    }

    [Fact]
    public async Task BuildAsync_AdminOverridesPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<IInventoryListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(true);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync(new List<Inventory>());

        var service = new WorkspaceInventoryViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.IsWorkspaceAdmin);
        Assert.True(result.CanCreateInventory);
        Assert.True(result.CanEditInventory);
    }

    [Fact]
    public async Task BuildAsync_DefaultsPermissionsWhenNotFound()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<IInventoryListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync(new List<Inventory>());

        var service = new WorkspaceInventoryViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.IsWorkspaceAdmin);
        Assert.False(result.CanCreateInventory);
        Assert.False(result.CanEditInventory);
        Assert.Empty(result.Items);
    }
}

