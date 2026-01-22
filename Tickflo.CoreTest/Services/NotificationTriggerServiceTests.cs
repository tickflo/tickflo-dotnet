namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class NotificationTriggerServiceTests
{
    [Fact]
    public async Task NotifyTicketAssignmentChangedAsyncAddsUnassignAndAssign()
    {
        var repo = new Mock<INotificationRepository>();
        var svc = new NotificationTriggerService(
            repo.Object,
            Mock.Of<IUserRepository>(),
            Mock.Of<ITeamRepository>(),
            Mock.Of<IWorkspaceRepository>(),
            Mock.Of<IContactRepository>()
        );

        var ticket = new Ticket { AssignedUserId = 7 };
        await svc.NotifyTicketAssignmentChangedAsync(1, ticket, previousUserId: 5, previousTeamId: null, changedByUserId: 3);

        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == "ticket_unassigned" && n.UserId == 5)), Times.Once);
        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == "ticket_assigned" && n.UserId == 7)), Times.Once);
    }
}
