using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task CreateAsync_Persists_Notification()
    {
        var repo = new Mock<INotificationRepository>();
        var email = new Mock<IEmailSender>();
        var svc = new NotificationService(repo.Object, email.Object);

        await svc.CreateAsync(5, "type", "subject", "body");

        repo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.UserId == 5 && n.Status == "pending")), Times.Once);
    }

    [Fact]
    public async Task SendPendingInApp_Marks_Sent()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.ListPendingAsync("in_app", 100)).ReturnsAsync(new List<Notification> { new() { Id = 1 } });
        var svc = new NotificationService(repo.Object, Mock.Of<IEmailSender>());

        await svc.SendPendingInAppAsync();

        repo.Verify(r => r.MarkAsSentAsync(1), Times.Once);
    }
}
