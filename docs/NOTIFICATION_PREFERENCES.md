# User Notification Preferences

## Overview

This document describes the user notification preferences system that allows users to control which notifications they receive and through which delivery methods (Email, In-App, SMS, Push).

## Database Schema

### Table: `user_notification_preferences`

Stores per-user, per-notification-type delivery preferences.

```sql
CREATE TABLE user_notification_preferences (
    user_id integer NOT NULL,
    notification_type varchar(50) NOT NULL,
    email_enabled boolean DEFAULT true NOT NULL,
    in_app_enabled boolean DEFAULT true NOT NULL,
    sms_enabled boolean DEFAULT false NOT NULL,
    push_enabled boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone,
    PRIMARY KEY (user_id, notification_type),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
```

**Key Features:**
- Composite primary key: (user_id, notification_type)
- Cascade delete when user is removed
- Defaults: email and in-app enabled, SMS and push disabled
- If no preference exists for a user/type combination, system defaults apply

## Notification Types

The system supports 8 notification types:

| Type | Description | Default Email | Default In-App | Default SMS | Default Push |
|------|-------------|---------------|----------------|-------------|--------------|
| `workspace_invite` | User invited to workspace | ✅ | ✅ | ❌ | ❌ |
| `ticket_assigned` | Ticket assigned to user | ✅ | ✅ | ❌ | ❌ |
| `ticket_comment` | Comment on user's ticket | ✅ | ✅ | ❌ | ❌ |
| `ticket_status_change` | Ticket status changed | ✅ | ✅ | ❌ | ❌ |
| `report_completed` | Report finished processing | ✅ | ✅ | ❌ | ❌ |
| `mention` | User mentioned in comment | ✅ | ✅ | ❌ | ❌ |
| `ticket_summary` | Daily ticket summary | ✅ | ✅ | ❌ | ❌ |
| `password_reset` | Password reset confirmation | ✅ | ✅ | ❌ | ❌ |

## Delivery Methods

Four delivery methods are supported:

1. **Email** - Traditional email notifications (default: enabled)
2. **In-App** - Browser/app notifications (default: enabled)
3. **SMS** - Text message notifications (default: disabled, requires SMS integration)
4. **Push** - Mobile push notifications (default: disabled, requires push integration)

## User Interface

### Profile Page: `/profile`

Users can manage their notification preferences from their profile page. The UI includes:

- **Notification Table**: Grid showing all notification types with checkboxes for each delivery method
- **Save Changes Button**: Persists updated preferences
- **Info Alert**: Warns users that disabling all methods means they won't receive that notification

**Layout:**
```
Notification Type       | Email | In-App | SMS | Push |
------------------------|-------|--------|-----|------|
Workspace Invitation    |   ☑   |   ☑    |  ☐  |  ☐   |
Ticket Assigned to You  |   ☑   |   ☑    |  ☐  |  ☐   |
...
```

## Code Implementation

### Entity: `UserNotificationPreference.cs`

```csharp
public class UserNotificationPreference
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = "";
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### Repository Interface: `IUserNotificationPreferenceRepository.cs`

```csharp
public interface IUserNotificationPreferenceRepository
{
    Task<List<UserNotificationPreference>> GetPreferencesForUserAsync(int userId);
    Task<UserNotificationPreference?> GetPreferenceAsync(int userId, string notificationType);
    Task SavePreferenceAsync(UserNotificationPreference preference);
    Task SavePreferencesAsync(IEnumerable<UserNotificationPreference> preferences);
    Task ResetToDefaultsAsync(int userId);
}
```

### Repository: `UserNotificationPreferenceRepository.cs`

Key methods:
- **GetPreferencesForUserAsync**: Retrieve all preferences for a user
- **GetPreferenceAsync**: Get specific preference by user + type
- **SavePreferenceAsync**: Upsert single preference (update if exists, insert if new)
- **SavePreferencesAsync**: Bulk upsert for multiple preferences
- **ResetToDefaultsAsync**: Delete all user preferences (system defaults will apply)

**Upsert Pattern:**
```csharp
var existing = await GetPreferenceAsync(preference.UserId, preference.NotificationType);
if (existing != null)
{
    // Update existing
    existing.EmailEnabled = preference.EmailEnabled;
    existing.InAppEnabled = preference.InAppEnabled;
    existing.SmsEnabled = preference.SmsEnabled;
    existing.PushEnabled = preference.PushEnabled;
    existing.UpdatedAt = DateTime.UtcNow;
}
else
{
    // Insert new
    _context.UserNotificationPreferences.Add(preference);
}
```

### Page Model: `Profile.cshtml.cs`

**Key Properties:**
- `NotificationPreferences`: List of preference items for display
- `EmailPrefs`, `InAppPrefs`, `SmsPrefs`, `PushPrefs`: Dictionaries for form binding

**OnGetAsync:**
1. Load user information
2. Fetch existing preferences from database
3. Build preference list for all 8 notification types
4. Apply defaults for types without saved preferences
5. Populate dictionaries for form binding

**OnPostAsync:**
1. Update user profile information
2. Loop through all notification types
3. Create/update preference entities from form data
4. Bulk save all preferences via repository

### View: `Profile.cshtml`

Razor page with:
- User profile section (avatar, name, email)
- Notification preferences table with checkboxes
- Save button to persist changes
- Info alert about disabling all methods

## Usage Example

### Checking User Preferences Before Sending

```csharp
public async Task SendNotificationAsync(int userId, string notificationType, NotificationData data)
{
    // Get user preference
    var pref = await _notificationPrefs.GetPreferenceAsync(userId, notificationType);
    
    // Apply defaults if no preference exists
    bool emailEnabled = pref?.EmailEnabled ?? true;
    bool inAppEnabled = pref?.InAppEnabled ?? true;
    bool smsEnabled = pref?.SmsEnabled ?? false;
    bool pushEnabled = pref?.PushEnabled ?? false;
    
    // Create notifications only for enabled methods
    if (emailEnabled)
    {
        await _notificationService.CreateAsync(new Notification
        {
            UserId = userId,
            Type = notificationType,
            DeliveryMethod = "email",
            // ... other fields
        });
    }
    
    if (inAppEnabled)
    {
        await _notificationService.CreateAsync(new Notification
        {
            UserId = userId,
            Type = notificationType,
            DeliveryMethod = "in_app",
            // ... other fields
        });
    }
    
    // ... SMS and Push similarly
}
```

## Seed Data

Sample preferences are provided for demo users showing varied preference patterns:

- **User 1 (John Admin)**: Email + In-App for everything
- **User 2 (Jane Smith)**: Only urgent notifications via email
- **User 3 (Bob Johnson)**: In-app only, no emails
- **User 4 (Alice Williams)**: All channels for critical items
- **User 5 (Charlie Brown)**: Mixed preferences

Users without saved preferences automatically use system defaults.

## Migration Guide

### Adding Preferences to Existing Users

If users already exist in the database, no action is required. Preferences will use system defaults until users customize them via the profile page.

### Enforcing Preferences in Notification Service

To respect user preferences when creating notifications, modify `NotificationService.CreateAsync`:

```csharp
public async Task<Notification> CreateAsync(Notification notification)
{
    // Check user preference before creating
    var pref = await _notificationPrefs.GetPreferenceAsync(
        notification.UserId, 
        notification.Type
    );
    
    // Determine if this delivery method is enabled
    bool isEnabled = notification.DeliveryMethod switch
    {
        "email" => pref?.EmailEnabled ?? true,
        "in_app" => pref?.InAppEnabled ?? true,
        "sms" => pref?.SmsEnabled ?? false,
        "push" => pref?.PushEnabled ?? false,
        _ => true
    };
    
    // Only create if enabled
    if (!isEnabled)
    {
        return null; // Or throw exception
    }
    
    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();
    return notification;
}
```

## Benefits

1. **User Control**: Users can customize their notification experience
2. **Reduced Noise**: Users can disable unwanted notification channels
3. **Flexibility**: Different preferences per notification type
4. **Compliance**: Helps with email marketing compliance (CAN-SPAM, GDPR)
5. **Multi-Channel**: Supports future SMS and push integrations

## Future Enhancements

Potential improvements:
- **Quiet Hours**: Set time ranges when notifications should not be sent
- **Frequency Controls**: Limit notification frequency (e.g., digest instead of real-time)
- **Priority-Based**: Allow users to set thresholds (e.g., only high/urgent notifications)
- **Workspace-Specific**: Different preferences per workspace
- **Category Grouping**: Group notification types into categories for easier management
- **Preview Mode**: Show sample notifications before saving preferences
