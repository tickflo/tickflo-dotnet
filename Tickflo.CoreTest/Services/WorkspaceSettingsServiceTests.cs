namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;
using Xunit;

public class WorkspaceSettingsServiceTests
{
    [Fact]
    public async Task UpdateWorkspaceBasicSettingsAsyncThrowsOnSlugConflict()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        workspaceRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Workspace { Id = 1, Slug = "old" });
        workspaceRepository.Setup(r => r.FindBySlugAsync("new")).ReturnsAsync(new Workspace { Id = 2, Slug = "new" });
        var svc = new WorkspaceSettingsService(workspaceRepository.Object, Mock.Of<ITicketStatusRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<ITicketTypeRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateWorkspaceBasicSettingsAsync(1, "Name", "new"));
    }

    [Fact]
    public async Task EnsureDefaultsExistAsyncCreatesDefaultStatusesWhenEmpty()
    {
        var workspaceRepository = Mock.Of<IWorkspaceRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);
        statusRepository.Setup(r => r.CreateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketStatus s, CancellationToken _) => s);
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);
        priorityRepository.Setup(r => r.CreateAsync(It.IsAny<TicketPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketPriority p, CancellationToken _) => p);
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        ticketTypeRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([]);
        ticketTypeRepository.Setup(r => r.CreateAsync(It.IsAny<TicketType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketType t, CancellationToken _) => t);

        var svc = new WorkspaceSettingsService(workspaceRepository, statusRepository.Object, priorityRepository.Object, ticketTypeRepository.Object);
        await svc.EnsureDefaultsExistAsync(1);

        statusRepository.Verify(r => r.CreateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        priorityRepository.Verify(r => r.CreateAsync(It.IsAny<TicketPriority>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        ticketTypeRepository.Verify(r => r.CreateAsync(It.IsAny<TicketType>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EnsureDefaultsExistAsyncDoesNotCreateWhenDefaultsExist()
    {
        var workspaceRepository = Mock.Of<IWorkspaceRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([new() { Name = "New" }]);
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([new() { Name = "Normal" }]);
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        ticketTypeRepository.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync([new() { Name = "Bug" }]);

        var svc = new WorkspaceSettingsService(workspaceRepository, statusRepository.Object, priorityRepository.Object, ticketTypeRepository.Object);
        await svc.EnsureDefaultsExistAsync(1);

        statusRepository.Verify(r => r.CreateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()), Times.Never);
        priorityRepository.Verify(r => r.CreateAsync(It.IsAny<TicketPriority>(), It.IsAny<CancellationToken>()), Times.Never);
        ticketTypeRepository.Verify(r => r.CreateAsync(It.IsAny<TicketType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateWorkspaceBasicSettingsAsyncUpdatesNameAndSlug()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        var workspace = new Workspace { Id = 1, Slug = "old-slug", Name = "Old Name" };
        workspaceRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(workspace);
        workspaceRepository.Setup(r => r.FindBySlugAsync("new-slug")).ReturnsAsync((Workspace?)null);
        workspaceRepository.Setup(r => r.UpdateAsync(It.IsAny<Workspace>())).Returns(Task.CompletedTask);

        var svc = new WorkspaceSettingsService(workspaceRepository.Object, Mock.Of<ITicketStatusRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<ITicketTypeRepository>());

        await svc.UpdateWorkspaceBasicSettingsAsync(1, "New Name", "new-slug");

        Assert.Equal("New Name", workspace.Name);
        Assert.Equal("new-slug", workspace.Slug);
        workspaceRepository.Verify(r => r.UpdateAsync(workspace), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateSettingsAsyncUpdatesWorkspaceSettings()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        var workspace = new Workspace { Id = 1, Slug = "old-slug", Name = "Old Name" };
        workspaceRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(workspace);
        workspaceRepository.Setup(r => r.FindBySlugAsync("new-slug")).ReturnsAsync((Workspace?)null);
        workspaceRepository.Setup(r => r.UpdateAsync(It.IsAny<Workspace>())).Returns(Task.CompletedTask);

        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var typeRepository = new Mock<ITicketTypeRepository>();
        typeRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var svc = new WorkspaceSettingsService(
            workspaceRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            typeRepository.Object);

        var request = new BulkSettingsUpdateRequest
        {
            WorkspaceName = "New Name",
            WorkspaceSlug = "new-slug"
        };

        var result = await svc.BulkUpdateSettingsAsync(1, request);

        Assert.NotNull(result.UpdatedWorkspace);
        Assert.Equal("New Name", result.UpdatedWorkspace.Name);
        Assert.Equal("new-slug", result.UpdatedWorkspace.Slug);
        Assert.Equal(1, result.ChangesApplied);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task BulkUpdateSettingsAsyncUpdatesStatuses()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var existingStatus = new TicketStatus { Id = 1, WorkspaceId = 1, Name = "Old", Color = "blue", SortOrder = 1, IsClosedState = false };
        statusRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([existingStatus]);
        statusRepository.Setup(r => r.FindByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(existingStatus);
        statusRepository.Setup(r => r.UpdateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketStatus s, CancellationToken _) => s);

        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var typeRepository = new Mock<ITicketTypeRepository>();
        typeRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var svc = new WorkspaceSettingsService(
            workspaceRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            typeRepository.Object);

        var request = new BulkSettingsUpdateRequest
        {
            StatusUpdates =
            [
                new StatusUpdate { Id = 1, Name = "Updated", Color = "red", SortOrder = 2, IsClosedState = true }
            ]
        };

        var result = await svc.BulkUpdateSettingsAsync(1, request);

        Assert.Equal(1, result.ChangesApplied);
        Assert.Empty(result.Errors);
        Assert.Equal("Updated", existingStatus.Name);
        Assert.Equal("red", existingStatus.Color);
        Assert.Equal(2, existingStatus.SortOrder);
        Assert.True(existingStatus.IsClosedState);
    }

    [Fact]
    public async Task BulkUpdateSettingsAsyncDeletesStatuses()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var existingStatus = new TicketStatus { Id = 1, WorkspaceId = 1, Name = "ToDelete", Color = "blue", SortOrder = 1 };
        statusRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([existingStatus]);
        statusRepository.Setup(r => r.DeleteAsync(1, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var typeRepository = new Mock<ITicketTypeRepository>();
        typeRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var svc = new WorkspaceSettingsService(
            workspaceRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            typeRepository.Object);

        var request = new BulkSettingsUpdateRequest
        {
            StatusUpdates = [new StatusUpdate { Id = 1, Delete = true }]
        };

        var result = await svc.BulkUpdateSettingsAsync(1, request);

        Assert.Equal(1, result.ChangesApplied);
        statusRepository.Verify(r => r.DeleteAsync(1, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateSettingsAsyncCreatesNewStatus()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        statusRepository.Setup(r => r.FindByNameAsync(1, "New Status", It.IsAny<CancellationToken>())).ReturnsAsync((TicketStatus?)null);
        statusRepository.Setup(r => r.CreateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketStatus s, CancellationToken _) => s);

        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var typeRepository = new Mock<ITicketTypeRepository>();
        typeRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var svc = new WorkspaceSettingsService(
            workspaceRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            typeRepository.Object);

        var request = new BulkSettingsUpdateRequest
        {
            NewStatus = new StatusCreate { Name = "New Status", Color = "green", IsClosedState = false }
        };

        var result = await svc.BulkUpdateSettingsAsync(1, request);

        Assert.Equal(1, result.ChangesApplied);
        statusRepository.Verify(r => r.CreateAsync(It.Is<TicketStatus>(s => s.Name == "New Status" && s.Color == "green"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateSettingsAsyncHandlesErrors()
    {
        var workspaceRepository = new Mock<IWorkspaceRepository>();
        var workspace = new Workspace { Id = 1, Slug = "old", Name = "Old" };
        workspaceRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(workspace);
        workspaceRepository.Setup(r => r.FindBySlugAsync("taken")).ReturnsAsync(new Workspace { Id = 2, Slug = "taken" });

        var statusRepository = new Mock<ITicketStatusRepository>();
        statusRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var priorityRepository = new Mock<ITicketPriorityRepository>();
        priorityRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var typeRepository = new Mock<ITicketTypeRepository>();
        typeRepository.Setup(r => r.ListAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var svc = new WorkspaceSettingsService(
            workspaceRepository.Object,
            statusRepository.Object,
            priorityRepository.Object,
            typeRepository.Object);

        var request = new BulkSettingsUpdateRequest
        {
            WorkspaceSlug = "taken"
        };

        var result = await svc.BulkUpdateSettingsAsync(1, request);

        Assert.Equal(0, result.ChangesApplied);
        Assert.NotEmpty(result.Errors);
    }
}
