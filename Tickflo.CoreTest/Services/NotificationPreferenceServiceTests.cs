namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class NotificationPreferenceServiceTests
{
    [Fact]
    public async Task GetUserPreferencesAsyncInitializesDefaultsWhenNone()
    {
        var repo = new Mock<IUserNotificationPreferenceRepository>();
        repo.Setup(r => r.GetPreferencesForUserAsync(10)).ReturnsAsync([]);
        repo.Setup(r => r.SavePreferencesAsync(It.IsAny<List<UserNotificationPreference>>())).Returns(Task.CompletedTask);

        var svc = new NotificationPreferenceService(repo.Object);
        var prefs = await svc.GetUserPreferencesAsync(10);

        Assert.NotEmpty(prefs);
        repo.Verify(r => r.SavePreferencesAsync(It.IsAny<List<UserNotificationPreference>>()), Times.Once);
    }
}
