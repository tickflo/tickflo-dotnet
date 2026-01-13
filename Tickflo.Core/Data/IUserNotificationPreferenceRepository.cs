using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IUserNotificationPreferenceRepository
{
    Task<List<UserNotificationPreference>> GetPreferencesForUserAsync(int userId);
    Task<UserNotificationPreference?> GetPreferenceAsync(int userId, string notificationType);
    Task SavePreferenceAsync(UserNotificationPreference preference);
    Task SavePreferencesAsync(List<UserNotificationPreference> preferences);
    Task ResetToDefaultsAsync(int userId);
}
