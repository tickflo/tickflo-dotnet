namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserNotificationPreferenceRepository(TickfloDbContext dbContext) : IUserNotificationPreferenceRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<List<UserNotificationPreference>> GetPreferencesForUserAsync(int userId) => await this.dbContext.UserNotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();

    public async Task<UserNotificationPreference?> GetPreferenceAsync(int userId, string notificationType) => await this.dbContext.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == notificationType);

    public async Task SavePreferenceAsync(UserNotificationPreference preference)
    {
        var existing = await this.GetPreferenceAsync(preference.UserId, preference.NotificationType);

        if (existing != null)
        {
            existing.EmailEnabled = preference.EmailEnabled;
            existing.InAppEnabled = preference.InAppEnabled;
            existing.SmsEnabled = preference.SmsEnabled;
            existing.PushEnabled = preference.PushEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
            this.dbContext.UserNotificationPreferences.Update(existing);
        }
        else
        {
            preference.CreatedAt = DateTime.UtcNow;
            this.dbContext.UserNotificationPreferences.Add(preference);
        }

        await this.dbContext.SaveChangesAsync();
    }

    public async Task SavePreferencesAsync(List<UserNotificationPreference> preferences)
    {
        foreach (var pref in preferences)
        {
            await this.SavePreferenceAsync(pref);
        }
    }

    public async Task ResetToDefaultsAsync(int userId)
    {
        var existing = await this.GetPreferencesForUserAsync(userId);
        this.dbContext.UserNotificationPreferences.RemoveRange(existing);
        await this.dbContext.SaveChangesAsync();
    }
}
