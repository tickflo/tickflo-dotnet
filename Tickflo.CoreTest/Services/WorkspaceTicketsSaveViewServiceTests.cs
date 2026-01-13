using Moq;
using Xunit;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.CoreTest.Services;

public class WorkspaceTicketsSaveViewServiceTests
{
    [Fact]
    public async Task BuildAsync_AdminCanCreateAndEdit()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var ticketService = new Mock<ITicketManagementService>();

        uwr.Setup(x => x.IsAdminAsync(1, 10)).ReturnsAsync(true);

        var svc = new WorkspaceTicketsSaveViewService(uwr.Object, perms.Object, ticketService.Object);
        var result = await svc.BuildAsync(10, 1, true);

        Assert.True(result.CanCreateTickets);
        Assert.True(result.CanEditTickets);
        Assert.True(result.CanAccessTicket);
    }

    [Fact]
    public async Task BuildAsync_NonAdminCreateWithPerms()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var ticketService = new Mock<ITicketManagementService>();

        uwr.Setup(x => x.IsAdminAsync(2, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 2))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = true, CanEdit = false } }
            });

        var svc = new WorkspaceTicketsSaveViewService(uwr.Object, perms.Object, ticketService.Object);
        var result = await svc.BuildAsync(10, 2, true);

        Assert.True(result.CanCreateTickets);
        Assert.False(result.CanEditTickets);
        Assert.True(result.CanAccessTicket);
    }

    [Fact]
    public async Task BuildAsync_NonAdminEditWithPermsAndAccess()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var ticketService = new Mock<ITicketManagementService>();

        uwr.Setup(x => x.IsAdminAsync(3, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 3))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = false, CanEdit = true } }
            });

        var ticket = new Ticket { Id = 5, WorkspaceId = 10, Subject = "Test" };
        ticketService.Setup(x => x.CanUserAccessTicketAsync(ticket, 3, 10, false))
            .ReturnsAsync(true);

        var svc = new WorkspaceTicketsSaveViewService(uwr.Object, perms.Object, ticketService.Object);
        var result = await svc.BuildAsync(10, 3, false, ticket);

        Assert.False(result.CanCreateTickets);
        Assert.True(result.CanEditTickets);
        Assert.True(result.CanAccessTicket);
    }

    [Fact]
    public async Task BuildAsync_NonAdminDeniedEdit_NoPerms()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var ticketService = new Mock<ITicketManagementService>();

        uwr.Setup(x => x.IsAdminAsync(4, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 4))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>());

        var svc = new WorkspaceTicketsSaveViewService(uwr.Object, perms.Object, ticketService.Object);
        var result = await svc.BuildAsync(10, 4, false);

        Assert.False(result.CanCreateTickets);
        Assert.False(result.CanEditTickets);
        Assert.True(result.CanAccessTicket);
    }

    [Fact]
    public async Task BuildAsync_NonAdminEditDeniedByScope()
    {
        var uwr = new Mock<IUserWorkspaceRoleRepository>();
        var perms = new Mock<IRolePermissionRepository>();
        var ticketService = new Mock<ITicketManagementService>();

        uwr.Setup(x => x.IsAdminAsync(5, 10)).ReturnsAsync(false);
        perms.Setup(x => x.GetEffectivePermissionsForUserAsync(10, 5))
            .ReturnsAsync(new Dictionary<string, EffectiveSectionPermission>
            {
                { "tickets", new EffectiveSectionPermission { Section = "tickets", CanCreate = false, CanEdit = true } }
            });

        var ticket = new Ticket { Id = 7, WorkspaceId = 10, Subject = "Test" };
        ticketService.Setup(x => x.CanUserAccessTicketAsync(ticket, 5, 10, false))
            .ReturnsAsync(false);

        var svc = new WorkspaceTicketsSaveViewService(uwr.Object, perms.Object, ticketService.Object);
        var result = await svc.BuildAsync(10, 5, false, ticket);

        Assert.False(result.CanCreateTickets);
        Assert.True(result.CanEditTickets);
        Assert.False(result.CanAccessTicket);
    }
}

