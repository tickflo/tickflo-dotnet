using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceSettingsViewServiceTests
{
    [Fact]
    public async Task BuildAsync_AdminGetsAllPermissions_AndListsLoaded()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var settingsService = new Mock<IWorkspaceSettingsService>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);
        settingsService.Setup(x => x.EnsureDefaultsExistAsync(10)).Returns(Task.CompletedTask);

        statusRepo.Setup(x => x.ListAsync(10, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<Tickflo.Core.Entities.TicketStatus> { new Tickflo.Core.Entities.TicketStatus { Id = 1, WorkspaceId = 10, Name = "New", Color = "#ccc" } });
        priorityRepo.Setup(x => x.ListAsync(10, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<Tickflo.Core.Entities.TicketPriority> { new Tickflo.Core.Entities.TicketPriority { Id = 2, WorkspaceId = 10, Name = "Normal", Color = "#ddd" } });
        typeRepo.Setup(x => x.ListAsync(10, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new List<Tickflo.Core.Entities.TicketType> { new Tickflo.Core.Entities.TicketType { Id = 3, WorkspaceId = 10, Name = "Standard", Color = "#eee" } });

        var svc = new WorkspaceSettingsViewService(uwr.Object, perms.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object, settingsService.Object);
        var result = await svc.BuildAsync(10, 1);

        Assert.True(result.CanViewSettings);
        Assert.True(result.CanEditSettings);
        Assert.True(result.CanCreateSettings);
        Assert.Single(result.Statuses);
        Assert.Single(result.Priorities);
        Assert.Single(result.Types);
        settingsService.Verify(x => x.EnsureDefaultsExistAsync(10), Times.Once);
    }

    [Fact]
    public async Task BuildAsync_NonAdminGetsEffectivePermissions()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var settingsService = new Mock<IWorkspaceSettingsService>();

        uwr.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "settings", new EffectiveSectionPermission { Section = "settings", CanView = true, CanEdit = false, CanCreate = false } }
            });

        statusRepo.Setup(x => x.ListAsync(10, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new List<Tickflo.Core.Entities.TicketStatus>());
        priorityRepo.Setup(x => x.ListAsync(10, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new List<Tickflo.Core.Entities.TicketPriority>());
        typeRepo.Setup(x => x.ListAsync(10, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(new List<Tickflo.Core.Entities.TicketType>());
        settingsService.Setup(x => x.EnsureDefaultsExistAsync(10)).Returns(Task.CompletedTask);

        var svc = new WorkspaceSettingsViewService(uwr.Object, perms.Object, statusRepo.Object, priorityRepo.Object, typeRepo.Object, settingsService.Object);
        var result = await svc.BuildAsync(10, 2);

        Assert.True(result.CanViewSettings);
        Assert.False(result.CanEditSettings);
        Assert.False(result.CanCreateSettings);
        settingsService.Verify(x => x.EnsureDefaultsExistAsync(10), Times.Once);
    }
}

