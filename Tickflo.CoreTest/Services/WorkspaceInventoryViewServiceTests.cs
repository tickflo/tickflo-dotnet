namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceInventoryViewServiceTests
{
    [Fact]
    public async Task BuildAsyncLoadsInventoryWithPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var contactListingService = new Mock<IInventoryListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "inventory", new EffectiveSectionPermission { Section = "inventory", CanCreate = true, CanEdit = true } }
        };

        var inventoryItems = new List<Inventory>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "Widget A", Status = "active" },
            new() { Id = 2, WorkspaceId = 1, Name = "Widget B", Status = "active" }
        };

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        contactListingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync(inventoryItems);

        var service = new WorkspaceInventoryViewService(accessService.Object, contactListingService.Object);

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
    public async Task BuildAsyncAdminOverridesPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var contactListingService = new Mock<IInventoryListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(true);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        contactListingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync([]);

        var service = new WorkspaceInventoryViewService(accessService.Object, contactListingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.IsWorkspaceAdmin);
        Assert.True(result.CanCreateInventory);
        Assert.True(result.CanEditInventory);
    }

    [Fact]
    public async Task BuildAsyncDefaultsPermissionsWhenNotFound()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var contactListingService = new Mock<IInventoryListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        contactListingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync([]);

        var service = new WorkspaceInventoryViewService(accessService.Object, contactListingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.IsWorkspaceAdmin);
        Assert.False(result.CanCreateInventory);
        Assert.False(result.CanEditInventory);
        Assert.Empty(result.Items);
    }
}

