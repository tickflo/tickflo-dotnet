using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class NotificationPreferenceServiceTests
{
    [Fact]
    public async Task GetUserPreferencesAsync_InitializesDefaults_WhenNone()
    {
        var repo = new Mock<IUserNotificationPreferenceRepository>();
        repo.Setup(r => r.GetPreferencesForUserAsync(10)).ReturnsAsync(new List<UserNotificationPreference>());
        repo.Setup(r => r.SavePreferencesAsync(It.IsAny<List<UserNotificationPreference>>())).Returns(Task.CompletedTask);

        var svc = new NotificationPreferenceService(repo.Object);
        var prefs = await svc.GetUserPreferencesAsync(10);

        Assert.NotEmpty(prefs);
        repo.Verify(r => r.SavePreferencesAsync(It.IsAny<List<UserNotificationPreference>>()), Times.Once);
    }
}
