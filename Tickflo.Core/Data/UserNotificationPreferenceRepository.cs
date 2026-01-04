using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class UserNotificationPreferenceRepository(TickfloDbContext db) : IUserNotificationPreferenceRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task<List<UserNotificationPreference>> GetPreferencesForUserAsync(int userId)
    {
        return await _db.UserNotificationPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserNotificationPreference?> GetPreferenceAsync(int userId, string notificationType)
    {
        return await _db.UserNotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == notificationType);
    }

    public async Task SavePreferenceAsync(UserNotificationPreference preference)
    {
        var existing = await GetPreferenceAsync(preference.UserId, preference.NotificationType);
        
        if (existing != null)
        {
            existing.EmailEnabled = preference.EmailEnabled;
            existing.InAppEnabled = preference.InAppEnabled;
            existing.SmsEnabled = preference.SmsEnabled;
            existing.PushEnabled = preference.PushEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
            _db.UserNotificationPreferences.Update(existing);
        }
        else
        {
            preference.CreatedAt = DateTime.UtcNow;
            _db.UserNotificationPreferences.Add(preference);
        }
        
        await _db.SaveChangesAsync();
    }

    public async Task SavePreferencesAsync(List<UserNotificationPreference> preferences)
    {
        foreach (var pref in preferences)
        {
            await SavePreferenceAsync(pref);
        }
    }

    public async Task ResetToDefaultsAsync(int userId)
    {
        var existing = await GetPreferencesForUserAsync(userId);
        _db.UserNotificationPreferences.RemoveRange(existing);
        await _db.SaveChangesAsync();
    }
}
