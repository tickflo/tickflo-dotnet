namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceLocationsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncLoadsLocationsWithPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<ILocationListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "locations", new EffectiveSectionPermission { Section = "locations", CanCreate = true, CanEdit = true } }
        };

        var locationItems = new List<ILocationListingService.LocationItem>
        {
            new() { Id = 1, Name = "New York" },
            new() { Id = 2, Name = "San Francisco" }
        };

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1))
            .ReturnsAsync(locationItems);

        var service = new WorkspaceLocationsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.CanCreateLocations);
        Assert.True(result.CanEditLocations);
        Assert.Equal(2, result.Locations.Count);
        Assert.Equal("New York", result.Locations[0].Name);
    }

    [Fact]
    public async Task BuildAsyncDefaultsPermissionsWhenNotFound()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<ILocationListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1))
            .ReturnsAsync([]);

        var service = new WorkspaceLocationsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateLocations);
        Assert.False(result.CanEditLocations);
        Assert.Empty(result.Locations);
    }
}

