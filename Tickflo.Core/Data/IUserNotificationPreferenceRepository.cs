namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IUserNotificationPreferenceRepository
{
    public Task<List<UserNotificationPreference>> GetPreferencesForUserAsync(int userId);
    public Task<UserNotificationPreference?> GetPreferenceAsync(int userId, string notificationType);
    public Task SavePreferenceAsync(UserNotificationPreference preference);
    public Task SavePreferencesAsync(List<UserNotificationPreference> preferences);
    public Task ResetToDefaultsAsync(int userId);
}
