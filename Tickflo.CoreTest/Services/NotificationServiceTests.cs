namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class NotificationServiceTests
{
    [Fact]
    public async Task CreateAsyncPersistsNotification()
    {
        var repo = new Mock<INotificationRepository>();
        var email = new Mock<IEmailSenderService>();
        var svc = new NotificationService(repo.Object);

        await svc.CreateAsync(5, "type", "subject", "body");

        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.UserId == 5 && n.Status == "pending")), Times.Once);
    }

    [Fact]
    public async Task SendPendingInAppMarksSent()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.ListPendingAsync("in_app", 100)).ReturnsAsync([new() { Id = 1 }]);
        var svc = new NotificationService(repo.Object);

        await svc.SendPendingInAppAsync();

        repo.Verify(r => r.MarkAsSentAsync(1), Times.Once);
    }
}
