using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Represents a notification type definition with its label.
/// </summary>
public class NotificationTypeDefinition
{
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing user notification preferences.
/// </summary>
public interface INotificationPreferenceService
{
    /// <summary>
    /// Gets all available notification type definitions.
    /// </summary>
    /// <returns>List of available notification types</returns>
    List<NotificationTypeDefinition> GetNotificationTypeDefinitions();

    /// <summary>
    /// Gets notification preferences for a user.
    /// Initializes default preferences if none exist.
    /// </summary>
    /// <param name="userId">The user to get preferences for</param>
    /// <returns>List of user notification preferences with defaults</returns>
    Task<List<UserNotificationPreference>> GetUserPreferencesAsync(int userId);

    /// <summary>
    /// Saves or updates notification preferences for a user.
    /// </summary>
    /// <param name="userId">The user</param>
    /// <param name="preferences">The preferences to save</param>
    /// <returns>The saved preferences</returns>
    Task<List<UserNotificationPreference>> SavePreferencesAsync(int userId, List<UserNotificationPreference> preferences);

    /// <summary>
    /// Initializes default notification preferences for a new user.
    /// </summary>
    /// <param name="userId">The user to initialize preferences for</param>
    /// <returns>The created default preferences</returns>
    Task<List<UserNotificationPreference>> InitializeDefaultPreferencesAsync(int userId);
}
