namespace Tickflo.Core.Services.Common;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of INotificationPreferenceService.
/// Manages user notification preferences and default initialization.
/// </summary>
public class NotificationPreferenceService(IUserNotificationPreferenceRepository preferenceRepository) : INotificationPreferenceService
{
    private readonly IUserNotificationPreferenceRepository _preferenceRepository = preferenceRepository;

    // Define all notification types as a constant to be reused
    private static readonly NotificationTypeDefinition[] DefaultNotificationTypes =
    [
        new NotificationTypeDefinition { Type = "workspace_invite", Label = "Workspace Invitation" },
        new NotificationTypeDefinition { Type = "ticket_assigned", Label = "Ticket Assigned to You" },
        new NotificationTypeDefinition { Type = "ticket_comment", Label = "Comments on Your Tickets" },
        new NotificationTypeDefinition { Type = "ticket_status_change", Label = "Ticket Status Changes" },
        new NotificationTypeDefinition { Type = "report_completed", Label = "Report Completed" },
        new NotificationTypeDefinition { Type = "mention", Label = "Mentions in Comments" },
        new NotificationTypeDefinition { Type = "ticket_summary", Label = "Daily Ticket Summary" },
        new NotificationTypeDefinition { Type = "password_reset", Label = "Password Reset Confirmation" },
    ];

    public List<NotificationTypeDefinition> GetNotificationTypeDefinitions() => [.. DefaultNotificationTypes];

    public async Task<List<UserNotificationPreference>> GetUserPreferencesAsync(int userId)
    {
        var existing = await this._preferenceRepository.GetPreferencesForUserAsync(userId);

        if (existing.Count == 0)
        {
            // Initialize defaults if no preferences exist
            return await this.InitializeDefaultPreferencesAsync(userId);
        }

        // Ensure all notification types have preferences (fill in missing ones with defaults)
        var prefsByType = existing.ToDictionary(p => p.NotificationType, p => p);
        var result = new List<UserNotificationPreference>();

        foreach (var definition in DefaultNotificationTypes)
        {
            if (prefsByType.TryGetValue(definition.Type, out var pref))
            {
                result.Add(pref);
            }
            else
            {
                // Create default preference for missing type
                result.Add(new UserNotificationPreference
                {
                    UserId = userId,
                    NotificationType = definition.Type,
                    EmailEnabled = true,
                    InAppEnabled = true,
                    SmsEnabled = false,
                    PushEnabled = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        return result;
    }

    public async Task<List<UserNotificationPreference>> SavePreferencesAsync(int userId, List<UserNotificationPreference> preferences)
    {
        // Ensure all preferences have the correct user ID and timestamps
        foreach (var pref in preferences)
        {
            pref.UserId = userId;
            pref.UpdatedAt = DateTime.UtcNow;

            // Set CreatedAt if not already set
            if (pref.CreatedAt == default)
            {
                pref.CreatedAt = DateTime.UtcNow;
            }
        }

        await this._preferenceRepository.SavePreferencesAsync(preferences);
        return preferences;
    }

    public async Task<List<UserNotificationPreference>> InitializeDefaultPreferencesAsync(int userId)
    {
        var preferences = DefaultNotificationTypes
            .Select(dt => new UserNotificationPreference
            {
                UserId = userId,
                NotificationType = dt.Type,
                EmailEnabled = true,  // Email opt-in by default
                InAppEnabled = true,  // In-app opt-in by default
                SmsEnabled = false,   // SMS opt-out by default (requires user action)
                PushEnabled = false,  // Push opt-out by default (requires user action)
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        await this._preferenceRepository.SavePreferencesAsync(preferences);
        return preferences;
    }
}


