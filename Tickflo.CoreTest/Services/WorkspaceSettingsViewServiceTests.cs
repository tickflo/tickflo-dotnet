namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class WorkspaceSettingsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncAdminGetsAllPermissionsAndListsLoaded()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var settingsService = new Mock<IWorkspaceSettingsService>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);
        settingsService.Setup(x => x.EnsureDefaultsExistAsync(10)).Returns(Task.CompletedTask);

        statusRepository.Setup(x => x.ListAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = 1, WorkspaceId = 10, Name = "New", Color = "#ccc" }]);
        priorityRepository.Setup(x => x.ListAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = 2, WorkspaceId = 10, Name = "Normal", Color = "#ddd" }]);
        ticketTypeRepository.Setup(x => x.ListAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = 3, WorkspaceId = 10, Name = "Standard", Color = "#eee" }]);

        var svc = new WorkspaceSettingsViewService(userWorkspaceRoleRepository.Object, perms.Object, statusRepository.Object, priorityRepository.Object, ticketTypeRepository.Object, settingsService.Object);
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
    public async Task BuildAsyncNonAdminGetsEffectivePermissions()
    {
        var userWorkspaceRoleRepository = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var settingsService = new Mock<IWorkspaceSettingsService>();

        userWorkspaceRoleRepository.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "settings", new EffectiveSectionPermission { Section = "settings", CanView = true, CanEdit = false, CanCreate = false } }
            });

        statusRepository.Setup(x => x.ListAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        priorityRepository.Setup(x => x.ListAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        ticketTypeRepository.Setup(x => x.ListAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        settingsService.Setup(x => x.EnsureDefaultsExistAsync(10)).Returns(Task.CompletedTask);

        var svc = new WorkspaceSettingsViewService(userWorkspaceRoleRepository.Object, perms.Object, statusRepository.Object, priorityRepository.Object, ticketTypeRepository.Object, settingsService.Object);
        var result = await svc.BuildAsync(10, 2);

        Assert.True(result.CanViewSettings);
        Assert.False(result.CanEditSettings);
        Assert.False(result.CanCreateSettings);
        settingsService.Verify(x => x.EnsureDefaultsExistAsync(10), Times.Once);
    }
}

