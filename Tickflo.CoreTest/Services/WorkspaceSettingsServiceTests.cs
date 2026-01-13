using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceSettingsServiceTests
{
    [Fact]
    public async Task UpdateWorkspaceBasicSettingsAsync_Throws_On_Slug_Conflict()
    {
        var workspaceRepo = new Mock<IWorkspaceRepository>();
        workspaceRepo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Workspace { Id = 1, Slug = "old" });
        workspaceRepo.Setup(r => r.FindBySlugAsync("new")).ReturnsAsync(new Workspace { Id = 2, Slug = "new" });
        var svc = new WorkspaceSettingsService(workspaceRepo.Object, Mock.Of<ITicketStatusRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<ITicketTypeRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateWorkspaceBasicSettingsAsync(1, "Name", "new"));
    }

    [Fact]
    public async Task EnsureDefaultsExistAsync_Creates_Default_Statuses_When_Empty()
    {
        var workspaceRepo = Mock.Of<IWorkspaceRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        statusRepo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<TicketStatus>());
        statusRepo.Setup(r => r.CreateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketStatus s, CancellationToken _) => s);
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        priorityRepo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<TicketPriority>());
        priorityRepo.Setup(r => r.CreateAsync(It.IsAny<TicketPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketPriority p, CancellationToken _) => p);
        var typeRepo = new Mock<ITicketTypeRepository>();
        typeRepo.Setup(r => r.ListAsync(1, CancellationToken.None)).ReturnsAsync(new List<TicketType>());
        typeRepo.Setup(r => r.CreateAsync(It.IsAny<TicketType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TicketType t, CancellationToken _) => t);

        var svc = new WorkspaceSettingsService(workspaceRepo, statusRepo.Object, priorityRepo.Object, typeRepo.Object);
        await svc.EnsureDefaultsExistAsync(1);

        statusRepo.Verify(r => r.CreateAsync(It.IsAny<TicketStatus>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        priorityRepo.Verify(r => r.CreateAsync(It.IsAny<TicketPriority>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        typeRepo.Verify(r => r.CreateAsync(It.IsAny<TicketType>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
