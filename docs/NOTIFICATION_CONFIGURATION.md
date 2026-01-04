# Notification Integration Configuration

## Overview

The notification system is designed as a **pluggable integration architecture** for open source flexibility. Delivery methods are optional integrations that can be enabled/disabled and configured with different providers. This allows organizations to:

- Use only the integrations they need
- Choose their preferred service providers
- Swap providers without changing core code
- Add custom integrations via plugins

## Architecture

```
Core Notification System (Built-in)
    ├── Email Integration (Built-in: SMTP)
    │   ├── SendGrid (Plugin)
    │   ├── Mailgun (Plugin)
    │   └── AWS SES (Plugin)
    ├── In-App (Built-in: Database)
    ├── SMS Integration (Plugin Required)
    │   ├── Twilio (Plugin)
    │   ├── AWS SNS (Plugin)
    │   ├── MessageBird (Plugin)
    │   └── Vonage/Nexmo (Plugin)
    └── Push Integration (Plugin Required)
        ├── Firebase Cloud Messaging (Plugin)
        ├── Apple Push Notification Service (Plugin)
        ├── OneSignal (Plugin)
        └── Pusher Beams (Plugin)
```

## Location

**Page:** [Tickflo.Web/Pages/Workspaces/Settings.cshtml](../Tickflo.Web/Pages/Workspaces/Settings.cshtml)

Access at: `/workspaces/{slug}/settings`

The notification integrations section appears at the bottom of the settings page.

## Built-in Integrations

These integrations are included with Tickflo and require no additional packages.

### Email (SMTP)
- **Provider:** Built-in SMTP client
- **Configuration:** appsettings.json
- **Status:** Always available
- **Use Case:** Standard email notifications

### In-App Notifications
- **Provider:** Database storage
- **Configuration:** None required
- **Status:** Always available
- **Use Case:** Browser/app notification center

## Plugin Integrations

These require installing additional NuGet packages and configuring credentials.

### SMS Integrations

Require installing provider-specific packages.

#### Twilio
- **Package:** `Tickflo.Notifications.Twilio` (to be created)
- **Configuration:** Account SID, Auth Token
- **Use Case:** Most popular SMS provider

#### AWS SNS
- **PacIntegration Configuration

Each integration can be enabled/disabled and configured with a specific provider.

#### Email Integration
- **Type:** Toggle + Provider selection
- **Default:** Enabled with SMTP
- **Property:** `EmailIntegrationEnabled`, `EmailProvider`
- **Providers:**
  - `smtp` - Built-in SMTP (default)
  - `sendgrid` - SendGrid plugin
  - `mailgun` - Mailgun plugin
  - `ses` - AWS SES plugin

#### In-App Notifications
- **Type:** Toggle only (no provider selection)
- **Default:** Enabled
- **Property:** `InAppNotificationsEnabled`
- **Note:** Uses internal database, no external provider needed

#### SMS Integration
- **Type:** Toggle + Provider selection
- **Default:** Disabled
- **Property:** `SmsIntegrationEnabled`, `SmsProvider`
- **Providers:**
  - `none` - Not configured (default)
  - `twilio` - Twilio plugin
  - `sns` - AWS SNS plugin
  - `messagebird` - MessageBird plugin
  - `vonage` - Vonage/Nexmo plugin
- **Requirements:** Install plugin package + configure credentials

#### Push Integration
- **Type:** Toggle + Provider selection
- **Default:** Disabled
- **Property:** `PushIntegrationEnabled`, `PushProvider`
- **Providers:**
  - `none` - Not configured (default)
  - `fcm` - Firebase Cloud Messaging plugin
  - `apns` - Apple Push Notification Service plugin
  - `onesignal` - OneSignal plugin
  - `pusher` - Pusher Beams plugin
- **Requirements:** Install plugin package + configure API keys
- **Use Case:** Unified push service

#### Pusher Beams
- **Package:** `Tickflo.Notifications.PusherBeams` (to be created)
- **Configuration:** Instance ID, secret key
- **Use Case:** Real-time features

## Settings Categories

### 1. General Settings

#### Enable Notifications
- **Type:** Toggle (boolean)
- **Default:** `true`
- **Description:** Master switch for all notification types in the workspace. When disabled, no notifications will be sent regardless of other settings.
- **Property:** `NotificationsEnabled`

### 2. Delivery Methods

Control which channels can be used to deliver notifications workspace-wide.

#### Email Notifications
- **Type:** Toggle (boolean)
- **Default:** `true`
- **Description:** Allow sending notifications via email
- **Property:** `EmailNotificationsEnabled`
- **Note:** Requires SMTP configuration

#### In-App Notifications
- **Type:** Toggle (boolean)
- **Default:** `true`
- **Description:** Show notifications within the application interface
- **Property:** `InAppNotificationsEnabled`
- **Note:** Displayed in notification bell/center

#### SMS Notifications
- **Type:** Toggle (boolean)
- **Default:** `false`
- **Description:** Send urgent notifications via SMS text messages
- **Property:** `SmsNotificationsEnabled`
- **Requirements:** SMS gateway integration (Twilio, AWS SNS, etc.)

#### Push Notifications
- **Type:** Toggle (boolean)
- **Default:** `false`
- **Description:** Send push notifications to mobile devices
- **Property:** `PushNotificationsEnabled`
- **Requirements:** Push notification service (Firebase, APNs, etc.)

### 3. Batch & Scheduling

#### Batch Notification Delay
- **Type:** Number (minutes)
- **Default:** `30`
- **Range:** 1-120 minutes
- **Description:** Time window for grouping similar notifications into batches
- **Property:** `BatchNotificationDelay`
- **Example:** If set to 30 minutes, multiple ticket comments on the same ticket within 30 minutes will be grouped into one notification

#### Daily Summary Time
- **Type:** Number (hour)
- **Default:** `9` (9:00 AM)
- **Range:** 0-23 (24-hour format)
- **Description:** Hour of day to send daily ticket summary notifications
- **Property:** `DailySummaryHour`
- **Example:** Set to 9 for 9:00 AM, 17 for 5:00 PM

### 4. Priority Rules

Control automatic priority assignment for notification types.

#### Mark Mentions as Urgent
- **Type:** Checkbox (boolean)
- **Default:** `true`
- **Description:** When users are @mentioned in comments, treat the notification as urgent priority
- **Property:** `MentionNotificationsUrgent`
- **Effect:** Urgent notifications bypass batching and are sent immediately

#### Mark Ticket Assignments as High Priority
- **Type:** Checkbox (boolean)
- **Default:** `true`
- **Description:** Notify users immediately when tickets are assigned to them with high priority
- **Property:** `TicketAssignmentNotificationsHigh`
- **Effect:** High priority notifications are sent faster than normal priority

## User vs Workspace Settings

**Important:** These are workspace-wide **default** settings. Individual users can override these preferences in their profile settings at `/profile`.
Creating Plugin Integrations

### Plugin Package Structure

```
Tickflo.Notifications.{Provider}/
├── {Provider}NotificationSender.cs
├── {Provider}Configuration.cs
├── ServiceCollectionExtensions.cs
└── Tickflo.Notifications.{Provider}.csproj
```

### Example: Twilio SMS Plugin
// Email Integration
[BindProperty]
public bool EmailIntegrationEnabled { get; set; } = true;
[BindProperty]
public string EmailProvider { get; set; } = "smtp";

// SMS Integration
[BindProperty]
public bool SmsIntegrationEnabled { get; set; } = false;
[BindProperty]
public string SmsProvider { get; set; } = "none";

// Push Integration
[BindProperty]
public bool PushIntegrationEnabled { get; set; } = false;
[BindProperty]
public string PushProvider { get; set; } = "none";

// In-App (no provider selection needed)
[BindProperty]
public bool InAppNotificationsEnabled { get; set; } = true;
```bash
dotnet new classlib -n Tickflo.Notifications.Twilio
cd Tickflo.Notifications.Twilio
dotnet add package Twilio
dotnet add reference ../Tickflo.Core/Tickflo.Core.csproj
```

#### 2. Implement ISmsNotificationSender

```csharp
using Tickflo.Core.Services.Notifications;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Tickflo.Notifications.Twilio;

public class TwilioNotificationSender : ISmsNotificationSender
{
    
    // Email Integration
    public bool EmailIntegrationEnabled { get; set; } = true;
    public string EmailProvider { get; set; } = "smtp";
    
    // SMS Integration
    public bool SmsIntegrationEnabled { get; set; } = false;
    public string SmsProvider { get; set; } = "none";
    
    // Push Integration
    public bool PushIntegrationEnabled { get; set; } = false;
    public string PushProvider { get; set; } = "none";
    
    // In-App
    public bool InAppNotificationsEnabled { get; set; } = true;
    
    // Scheduling
    public int BatchNotificationDelay { get; set; } = 30;
    public int DailySummaryHour { get; set; } = 9;
    
    -- Email Integration
    email_integration_enabled BOOLEAN DEFAULT true NOT NULL,
    email_provider VARCHAR(50) DEFAULT 'smtp' NOT NULL,
    
    -- SMS Integration
    sms_integration_enabled BOOLEAN DEFAULT false NOT NULL,
    sms_provider VARCHAR(50) DEFAULT 'none' NOT NULL,
    
    -- Push Integration
    push_integration_enabled BOOLEAN DEFAULT false NOT NULL,
    push_provider VARCHAR(50) DEFAULT 'none' NOT NULL,
    
    -- In-App
    in_app_notifications_enabled BOOLEAN DEFAULT true NOT NULL,
    
    -- Scheduling
    batch_notification_delay INTEGER DEFAULT 30 NOT NULL,
    daily_summary_hour INTEGER DEFAULT 9 NOT NULL,
    mention_notifications_urgent BOOLEAN DEFAULT true NOT NULL,
    ticket_assignment_notifications_high BOOLEAN DEFAULT true NOT NULL,
    
                body: message,
                from: new Twilio.Types.PhoneNumber(_config.FromNumber),
                to: new Twilio.Types.PhoneNumber(phoneNumber)
            );
            
            return messageResource.Status != MessageResource.StatusEnum.Failed;
        }
        catch
        {IntegrationEnabled = settings.EmailIntegrationEnabled;
    EmailProvider = settings.EmailProvider;
    SmsIntegrationEnabled = settings.SmsIntegrationEnabled;
    SmsProvider = settings.SmsProvider;
    PushIntegrationEnabled = settings.PushIntegrationEnabled;
    PushProvider = settings.PushProvider;
    InAppNotificationsEnabled = settings.InApp
            return false;
        }
    }
}
```
IntegrationEnabled = EmailIntegrationEnabled,
    EmailProvider = EmailProvider,
    SmsIntegrationEnabled = SmsIntegrationEnabled,
    SmsProvider = SmsProvider,
    PushIntegrationEnabled = PushIntegrationEnabled,
    PushProvider = PushProvider,
    InAppNotificationsEnabled = InApp
#### 3. Configuration Model

```csharp
namespace Tickflo.Notifications.Twilio;

public class TwilioConfiguration
{
    public string AccountSid { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public string FromNumber { get; set; } = "";
}
```

#### 4. Service Registration Extension

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tickflo.Notifications.Twilio;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTwilioNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var config = new TwilioConfiguration();
        configuration.GetSection("Twilio").Bind(config);
        
        services.AddSingleton(config);
        services.AddScoped<ISmsNotificationSender, TwilioNotificationSender>();
        
        return services;
    }
}
```

#### 5. Usage in Tickflo.Web

**Install Package:**
```bash
dotnet add package Tickflo.Notifications.Twilio
```

**Configure in appsettings.json:**
```json
{
  "Twilio": {
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "FromNumber": "+15551234567"
  }
}
```

**Register in Program.cs:**
```csharp
using Tickflo.Notifications.Twilio;

// Add after other services
if (builder.Configuration.GetValue<bool>("Twilio:Enabled"))
{
    builder.Services.AddTwilioNotifications(builder.Configuration);
}
```

### Interface Definitions

Core interfaces for plugin implementations:

```csharp
namespace Tickflo.Core.Services.Notifications;

public interface IEmailNotificationSender
{
    Task<bool> SendAsync(string to, string subject, string body);
}

public interface ISmsNotificationSender
{
    Task<bool> SendAsync(string phoneNumber, string message);
}

public interface IPushNotificationSender
{
    Task<bool> SendAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
}
```

## 
### Hierarchy

1. **Workspace Settings** (this page) - System-wide defaults
2. **User Preferences** (/profile) - Individual user overrides

### Example

- Workspace has `EmailNotificationsEnabled = true`
- User has `EmailEnabled = false` for `ticket_comment` type
- **Result:** User will NOT receive email notifications for ticket comments, even though workspace allows emails

## Implementation Details

### Current State

The settings are currently implemented with **in-memory defaults** only. When you save settings, they are acknowledged but not persisted to the database.

### Storage Location

```csharp
// Properties in Settings.cshtml.cs
[BindProperty]
public bool NotificationsEnabled { get; set; } = true;

[BindProperty]
public bool EmailNotificationsEnabled { get; set; } = true;
// ... etc
```

### POST Handler

```csharp
public async Task<IActionResult> OnPostSaveNotificationSettingsAsync(string slug)
{
    // TODO: Save to workspace_settings table
    TempData["NotificationSettingsSaved"] = true;
    return RedirectToPage("/Workspaces/Settings", new { slug });
}
```

## Future Implementation

To fully implement persistent storage, you would:

### 1. Create WorkspaceSettings Entity

```csharp
public class WorkspaceSettings
{
    public int WorkspaceId { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool InAppNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;
    public bool PushNotificationsEnabled { get; set; } = false;
    public int BatchNotificationDelay { get; set; } = 30;
    public int DailySummaryHour { get; set; } = 9;
    public bool MentionNotificationsUrgent { get; set; } = true;
    public bool TicketAssignmentNotificationsHigh { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}integration settings and route to appropriate providers:

```csharp
public async Task<Notification> CreateAsync(Notification notification)
{
    // Check workspace settings
    var workspaceSettings = await _workspaceSettingsRepo.GetAsync(notification.WorkspaceId);
    
    // Check if notifications are enabled
    if (workspaceSettings?.NotificationsEnabled == false)
    {
        return null; // Notifications disabled for workspace
    }
    
    // Check if integration is enabled for delivery method
    bool integrationEnabled = notification.DeliveryMethod switch
    {
        "email" => workspaceSettings?.EmailIntegrationEnabled ?? true,
        "in_app" => workspaceSettings?.InAppNotificationsEnabled ?? true,
        "sms" => workspaceSettings?.SmsIntegrationEnabled ?? false,
        "push" => workspaceSettings?.PushIntegrationEnabled ?? false,
        _ => true
    };
    
    if (!integrationEnabled)
    {
        return null; // Integration not enabled
    }
    
    // Determine which provider to use
    string? provider = notification.DeliveryMethod switch
    {
        "email" => workspaceSettings?.EmailProvider ?? "smtp",
        "sms" => workspaceSettings?.SmsProvider,
        "push" => workspaceSettings?.PushProvider,
        _ => null
    };
    
    // Check if provider is configured
    if (provider == "none")
    {
        return null; // No provider configured
    }
    
    // Apply priority rules
    if (notification.Type == "mention" && workspaceSettings?.MentionNotificationsUrgent == true)
    {
        notification.Priority = "urgent";
    }
    
    if (notification.Type == "ticket_assigned" && workspaceSettings?.TicketAssignmentNotificationsHigh == true)
    {
        notification.Priority = "high";
    }
    
    // Check user preferences (already implemented)
    var userPref = await _userNotificationPrefsRepo.GetPreferenceAsync(
        notification.UserId, 
        notification.Type
    );
    
    // User preference overrides workspace setting
    bool userAllows = notification.DeliveryMethod switch
    {
        "email" => userPref?.EmailEnabled ?? true,
        "in_app" => userPref?.InAppEnabled ?? true,
        "sms" => userPref?.SmsEnabled ?? false,
        "push" => userPref?.PushEnabled ?? false,
        _ => true
    };
    
    if (!userAllows)
    {
        return null; // User disabled this notification type/method
    }
    
    // Store provider in notification for routing during send
    notification.Data = notification.Data == null 
        ? $"{{\"provider\":\"{provider}\"}}"
        : notification.Data; // Or merge with existing data
    
    // Create notification
    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();
    return notification;
}
```

### Sending with Provider Routing

```csharp
public async Task SendPendingEmailsAsync(int limit = 50)
{
    var pending = await _notificationRepo.ListPendingAsync("email", limit);
    
    foreach (var notification in pending)
    {
        try
        {
            // Extract provider from notification data
            var provider = ExtractProvider(notification.Data) ?? "smtp";
            
            // Route to appropriate sender
            bool success = provider switch
            {
                "smtp" => await _smtpSender.SendAsync(notification),
                "sendgrid" => await _sendGridSender.SendAsync(notification),
                "mailgun" => await _mailgunSender.SendAsync(notification),
                "ses" => await _sesSender.SendAsync(notification),
                _ => await _smtpSender.SendAsync(notification) // fallback
            };
            
            if (success)
            {
                await _notificationRepo.MarkAsSentAsync(notification.Id);
            }
            else
            {
                await _notificationRepo.MarkAsFailedAsync(
                    notification.Id, 
                    $"Failed to send via {provider}"
                );
            }
        }
        catch (Exception ex)
        {
            await _notificationRepo.MarkAsFailedAsync(notification.Id, ex.Message);
        }
    }
}
```

## Benefits of Integration Architecture

1. **Flexibility**: Choose providers that fit your needs
2. **Cost Control**: Use only what you pay for
3. **Extensibility**: Easy to add new providers via plugins
4. **Open Source Friendly**: No vendor lock-in
5. **Testability**: Mock integrations in tests
6. **Gradual Adoption**: Start with built-ins, add integrations as needed
7. **Multi-Provider**: Use different providers per workspace
    if (!deliveryMethodAllowed)
    {
        return null; // Delivery method not enabled
    }
    
    // Apply priority rules
    if (notification.Type == "mention" && workspaceSettings?.MentionNotificationsUrgent == true)
    {
        notification.Priority = "urgent";
    }
    
    if (notification.Type == "ticket_assigned" && workspaceSettings?.TicketAssignmentNotificationsHigh == true)
    {
        notification.Priority = "high";
    }
    
    // Check user preferences (already implemented)
    var userPref = await _userNotificationPrefsRepo.GetPreferenceAsync(
        notification.UserId, 
        notification.Type
    );
    
    // User preference overrides workspace setting
    bool userAllows = notification.DeliveryMethod switch
    {
        "email" => userPref?.EmailEnabled ?? true,
        "in_app" => userPref?.InAppEnabled ?? true,
        "sms" => userPref?.SmsEnabled ?? false,
        "push" => userPref?.PushEnabled ?? false,
        _ => true
    };
    
    if (!userAllows)
    {
        return null; // User disabled this notification type/method
    }
    
    // Create notification
    _context.Notifications.Add(notification);
    await _context.SaveChangesAsync();
    return notification;
}
```

## Permissions

- **View Settings:** `settings:view` permission required
- **Edit Settings:** `settings:edit` permission required
- **Workspace Admins:** Have all permissions by default

## UI Features

- **Toggle Switches:** For boolean enable/disable settings
- **Number Inputs:** For delay and time settings with min/max validation
- **Checkboxes:** For priority rule settings
- **Info Alert:** Explains relationship with user preferences
- **Success Alert:** Shows confirmation when settings are saved
- **Disabled State:** Buttons disabled if user lacks permissions

## Related Documentation

- [Notification System](NOTIFICATION_SYSTEM.md) - Core notification architecture
- [Notification Preferences](NOTIFICATION_PREFERENCES.md) - User-level preferences
- [Settings Page](../Tickflo.Web/Pages/Workspaces/Settings.cshtml) - Full implementation
