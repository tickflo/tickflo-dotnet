using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class NotificationTriggerServiceTests
{
    [Fact]
    public async Task NotifyTicketAssignmentChangedAsync_Adds_Unassign_And_Assign()
    {
        var repo = new Mock<INotificationRepository>();
        var svc = new NotificationTriggerService(repo.Object, Mock.Of<IUserRepository>(), Mock.Of<ITeamRepository>(), Mock.Of<ILocationRepository>(), Mock.Of<IUserWorkspaceRepository>(), Mock.Of<IWorkspaceRepository>(), Mock.Of<IContactRepository>());

        var ticket = new Ticket { AssignedUserId = 7 };
        await svc.NotifyTicketAssignmentChangedAsync(1, ticket, previousUserId: 5, previousTeamId: null, changedByUserId: 3);

        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == "ticket_unassigned" && n.UserId == 5)), Times.Once);
        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == "ticket_assigned" && n.UserId == 7)), Times.Once);
    }
}
