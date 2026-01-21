namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class UserNotificationPreferenceRepository(TickfloDbContext db) : IUserNotificationPreferenceRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task<List<UserNotificationPreference>> GetPreferencesForUserAsync(int userId) => await this._db.UserNotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();

    public async Task<UserNotificationPreference?> GetPreferenceAsync(int userId, string notificationType) => await this._db.UserNotificationPreferences
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
            this._db.UserNotificationPreferences.Update(existing);
        }
        else
        {
            preference.CreatedAt = DateTime.UtcNow;
            this._db.UserNotificationPreferences.Add(preference);
        }

        await this._db.SaveChangesAsync();
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
        this._db.UserNotificationPreferences.RemoveRange(existing);
        await this._db.SaveChangesAsync();
    }
}
