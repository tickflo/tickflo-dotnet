using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Xunit;

namespace Tickflo.CoreTest.Services.Views;

/// <summary>
/// Tests for ClientPortalViewService.
/// </summary>
public class ClientPortalViewServiceTests
{
    [Fact]
    public async Task BuildAsync_LoadsContactTicketsOnly()
    {
        // Arrange
        var workspaceId = 1;
        var contactId = 10;
        var contact = new Contact { Id = contactId, WorkspaceId = workspaceId, Name = "John Doe" };
        var workspace = new Workspace { Id = workspaceId, Name = "TestWorkspace", Slug = "test" };

        var tickets = new List<Ticket>
        {
            new() { Id = 1, WorkspaceId = workspaceId, ContactId = contactId, Subject = "Ticket 1" },
            new() { Id = 2, WorkspaceId = workspaceId, ContactId = 99, Subject = "Other Contact Ticket" }, // Should be filtered
            new() { Id = 3, WorkspaceId = workspaceId, ContactId = contactId, Subject = "Ticket 3" }
        };

        var ticketRepo = new Mock<ITicketRepository>();
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();

        ticketRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tickets);
        workspaceRepo.Setup(r => r.FindByIdAsync(workspaceId))
            .ReturnsAsync(workspace);
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>());
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());

        var service = new ClientPortalViewService(ticketRepo.Object, workspaceRepo.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object);

        // Act
        var result = await service.BuildAsync(contact, workspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contact, result.Contact);
        Assert.Equal(workspace, result.Workspace);
        Assert.Equal(2, result.Tickets.Count); // Only 2 tickets for this contact
        Assert.All(result.Tickets, t => Assert.Equal(contactId, t.ContactId));
    }

    [Fact]
    public async Task BuildAsync_LoadsMetadataWithDefaults()
    {
        // Arrange
        var workspaceId = 1;
        var contact = new Contact { Id = 1, WorkspaceId = workspaceId, Name = "John" };
        var workspace = new Workspace { Id = workspaceId, Name = "Test", Slug = "test" };

        var ticketRepo = new Mock<ITicketRepository>();
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();

        ticketRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());
        workspaceRepo.Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(workspace);
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketStatus>()); // Empty - should use defaults
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>()); // Empty - should use defaults
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>()); // Empty - should use defaults

        var service = new ClientPortalViewService(ticketRepo.Object, workspaceRepo.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object);

        // Act
        var result = await service.BuildAsync(contact, workspaceId);

        // Assert
        Assert.NotEmpty(result.Statuses); // Should have default statuses
        Assert.NotEmpty(result.Priorities); // Should have default priorities
        Assert.NotEmpty(result.Types); // Should have default types
        Assert.NotEmpty(result.StatusColorByName);
        Assert.NotEmpty(result.PriorityColorByName);
        Assert.NotEmpty(result.TypeColorByName);
    }

    [Fact]
    public async Task BuildAsync_LoadsCustomMetadata()
    {
        // Arrange
        var workspaceId = 1;
        var contact = new Contact { Id = 1, WorkspaceId = workspaceId, Name = "John" };
        var workspace = new Workspace { Id = workspaceId, Name = "Test", Slug = "test" };

        var customStatuses = new List<TicketStatus>
        {
            new() { Name = "Custom Open", Color = "#FF0000", SortOrder = 1, IsClosedState = false }
        };
        var customPriorities = new List<TicketPriority>
        {
            new() { Name = "Critical", Color = "#FF0000", SortOrder = 1 }
        };
        var customTypes = new List<TicketType>
        {
            new() { Name = "Enhancement", Color = "#00FF00", SortOrder = 1 }
        };

        var ticketRepo = new Mock<ITicketRepository>();
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();

        ticketRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());
        workspaceRepo.Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(workspace);
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customStatuses);
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customPriorities);
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customTypes);

        var service = new ClientPortalViewService(ticketRepo.Object, workspaceRepo.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object);

        // Act
        var result = await service.BuildAsync(contact, workspaceId);

        // Assert
        Assert.Single(result.Statuses);
        Assert.Equal("Custom Open", result.Statuses[0].Name);
        Assert.Single(result.Priorities);
        Assert.Equal("Critical", result.Priorities[0].Name);
        Assert.Single(result.Types);
        Assert.Equal("Enhancement", result.Types[0].Name);
    }

    [Fact]
    public async Task BuildAsync_ThrowsWhenWorkspaceNotFound()
    {
        // Arrange
        var contact = new Contact { Id = 1, WorkspaceId = 99, Name = "John" };
        
        var ticketRepo = new Mock<ITicketRepository>();
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();

        workspaceRepo.Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Workspace?)null);

        var service = new ClientPortalViewService(ticketRepo.Object, workspaceRepo.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.BuildAsync(contact, 99)
        );
    }

    [Fact]
    public async Task BuildAsync_HandlesNullColors()
    {
        // Arrange
        var workspaceId = 1;
        var contact = new Contact { Id = 1, WorkspaceId = workspaceId, Name = "John" };
        var workspace = new Workspace { Id = workspaceId, Name = "Test", Slug = "test" };

        var statusesWithNullColor = new List<TicketStatus>
        {
            new() { Name = "Open", Color = string.Empty, SortOrder = 1, IsClosedState = false }
        };

        var ticketRepo = new Mock<ITicketRepository>();
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();

        ticketRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ticket>());
        workspaceRepo.Setup(r => r.FindByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(workspace);
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statusesWithNullColor);
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketPriority>());
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType>());

        var service = new ClientPortalViewService(ticketRepo.Object, workspaceRepo.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object);

        // Act
        var result = await service.BuildAsync(contact, workspaceId);

        // Assert
        Assert.Equal("neutral", result.StatusColorByName["Open"]); // Should default to "neutral"
    }
}
