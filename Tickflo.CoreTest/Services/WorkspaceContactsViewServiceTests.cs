namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceContactsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncLoadsContactsWithPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<IContactListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "contacts", new EffectiveSectionPermission { Section = "contacts", CanCreate = true, CanEdit = true } }
        };

        var contacts = new List<Contact>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "John Doe" },
            new() { Id = 2, WorkspaceId = 1, Name = "Jane Smith" }
        };

        var priorities = new List<TicketPriority>
        {
            new() { Id = 1, Name = "High", Color = "red" },
            new() { Id = 2, Name = "Low", Color = string.Empty }
        };

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1, "High", "John"))
            .ReturnsAsync((contacts, priorities));

        var service = new WorkspaceContactsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100, "High", "John");

        // Assert
        Assert.True(result.CanCreateContacts);
        Assert.True(result.CanEditContacts);
        Assert.Equal(2, result.Contacts.Count);
        Assert.Equal("John Doe", result.Contacts[0].Name);
        Assert.Equal(2, result.Priorities.Count);
        Assert.Equal("red", result.PriorityColorByName["High"]);
        Assert.Equal("neutral", result.PriorityColorByName["Low"]);
    }

    [Fact]
    public async Task BuildAsyncDefaultsPermissionsWhenNotFound()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<IContactListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync((new List<Contact>(), new List<TicketPriority>()));

        var service = new WorkspaceContactsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateContacts);
        Assert.False(result.CanEditContacts);
        Assert.Empty(result.Contacts);
        Assert.Empty(result.Priorities);
    }

    [Fact]
    public async Task BuildAsyncHandlesColorMappingCorrectly()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<IContactListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "contacts", new EffectiveSectionPermission { Section = "contacts", CanCreate = false, CanEdit = true } }
        };

        var priorities = new List<TicketPriority>
        {
            new() { Id = 1, Name = "Critical", Color = "dark-red" },
            new() { Id = 2, Name = "Normal", Color = string.Empty },
            new() { Id = 3, Name = "Minor", Color = string.Empty }
        };

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1, null, null))
            .ReturnsAsync((new List<Contact>(), priorities));

        var service = new WorkspaceContactsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanCreateContacts);
        Assert.True(result.CanEditContacts);
        Assert.Equal("dark-red", result.PriorityColorByName["Critical"]);
        Assert.Equal("neutral", result.PriorityColorByName["Normal"]);
        Assert.Equal("neutral", result.PriorityColorByName["Minor"]);
    }
}

