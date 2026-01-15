using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Tickets;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TicketManagementServiceTests
{
    [Fact]
    public async Task ValidateUserAssignmentAsync_Returns_True_When_Member()
    {
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindForWorkspaceAsync(1)).ReturnsAsync(new List<UserWorkspace> { new() { UserId = 4, Accepted = true } });
        var svc = new TicketManagementService(Mock.Of<ITicketRepository>(), Mock.Of<ITicketHistoryRepository>(), Mock.Of<IUserRepository>(), uw.Object, Mock.Of<ITeamRepository>(), Mock.Of<ITeamMemberRepository>(), Mock.Of<ILocationRepository>(), Mock.Of<IInventoryRepository>(), Mock.Of<IRolePermissionRepository>(), Mock.Of<ITicketTypeRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<ITicketStatusRepository>());

        var result = await svc.ValidateUserAssignmentAsync(4, 1);
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserAccessTicketAsync_Allows_Admin()
    {
        var ticket = new Ticket { AssignedUserId = 5 };
        var teamMembers = new Mock<ITeamMemberRepository>();
        var rolePerms = new Mock<IRolePermissionRepository>();
        var svc = new TicketManagementService(Mock.Of<ITicketRepository>(), Mock.Of<ITicketHistoryRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<ITeamRepository>(), teamMembers.Object, Mock.Of<ILocationRepository>(), Mock.Of<IInventoryRepository>(), rolePerms.Object, Mock.Of<ITicketTypeRepository>(), Mock.Of<ITicketPriorityRepository>(), Mock.Of<ITicketStatusRepository>());

        var allowed = await svc.CanUserAccessTicketAsync(ticket, 5, 1, isAdmin: true);
        Assert.True(allowed);
    }
}
