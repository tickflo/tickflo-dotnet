# Notification System Migration

## Overview
The email system has been migrated to a flexible notification system that supports multiple delivery methods (email, in-app, SMS, push) and batch processing.

## Changes Made

### 1. Database Schema Changes

#### Migration File: `db/migrations/20250822124430_import_existing.sql`
- **Removed Tables:**
  - `email_templates` - Template-based email system
  - `emails` - Email queue table

- **Added Table: `notifications`**
  ```sql
  CREATE TABLE notifications (
    id INTEGER PRIMARY KEY,
    workspace_id INTEGER,
    user_id INTEGER NOT NULL,
    type VARCHAR(50) NOT NULL,
    delivery_method VARCHAR(20) DEFAULT 'email',
    priority VARCHAR(20) DEFAULT 'normal',
    subject TEXT NOT NULL,
    body TEXT NOT NULL,
    data TEXT,                          -- JSON metadata
    status VARCHAR(20) DEFAULT 'pending',
    sent_at TIMESTAMP,
    failed_at TIMESTAMP,
    failure_reason TEXT,
    read_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL,
    created_by INTEGER,
    scheduled_for TIMESTAMP,
    batch_id VARCHAR(100)               -- For batch processing
  )
  ```

### 2. New Entities

#### `Tickflo.Core/Entities/Notification.cs`
Complete notification entity with properties for:
- Delivery method (email, sms, push, in_app, batch)
- Priority levels (low, normal, high, urgent)
- Status tracking (pending, sent, failed, cancelled)
- Read receipts for in-app notifications
- Batch processing support
- Scheduled delivery

### 3. Data Access Layer

#### `Tickflo.Core/Data/INotificationRepository.cs`
Interface with methods:
- `FindByIdAsync(int id)`
- `ListForUserAsync(int userId, bool unreadOnly)`
- `ListPendingAsync(string deliveryMethod, int limit)`
- `ListByBatchIdAsync(string batchId)`
- `AddAsync(Notification notification)`
- `UpdateAsync(Notification notification)`
- `MarkAsReadAsync(int id)`
- `MarkAsSentAsync(int id)`
- `MarkAsFailedAsync(int id, string reason)`
- `CountUnreadForUserAsync(int userId)`

#### `Tickflo.Core/Data/NotificationRepository.cs`
Full implementation with:
- Priority-based ordering (urgent → high → normal → low)
- Status filtering
- Batch operations support

### 4. Notification Service

#### `Tickflo.Core/Services/Notifications/NotificationService.cs`
Service layer providing:
- `CreateAsync()` - Create individual notifications
- `CreateBatchAsync()` - Create batch notifications with shared batch_id
- `SendPendingEmailsAsync()` - Process email queue
- `SendPendingInAppAsync()` - Mark in-app notifications as delivered

### 5. Service Registration

#### `Tickflo.Web/Program.cs`
Added registrations:
```csharp
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
```

### 6. Database Context

#### `Tickflo.Core/Data/TickfloDbContext.cs`
Added DbSet:
```csharp
public DbSet<Notification> Notifications => Set<Notification>();
```

### 7. Seed Data

#### `db/seed_data.sql`
Added sample notifications demonstrating:
- Email notifications (sent)
- In-app notifications (sent and pending)
- Batch notifications (pending)
- Failed notification with error reason
- Different notification types:
  - workspace_invite
  - ticket_assigned
  - ticket_comment
  - ticket_status_change
  - report_completed
  - mention
  - ticket_summary
  - password_reset

## Notification Types

The system supports various notification types:
- `workspace_invite` - Workspace invitation emails
- `ticket_assigned` - Ticket assignment alerts
- `ticket_comment` - New comment notifications
- `ticket_status_change` - Status update notifications
- `report_completed` - Report generation completion
- `mention` - User mention in comments
- `ticket_summary` - Digest/summary emails
- `password_reset` - Password reset requests

## Delivery Methods

- **email** - Traditional email delivery via SMTP
- **in_app** - Notifications shown in the application UI
- **sms** - SMS delivery (infrastructure ready)
- **push** - Push notifications (infrastructure ready)
- **batch** - Batched delivery for digest emails

## Priority Levels

- **urgent** - Immediate delivery (password resets, security alerts)
- **high** - High priority (workspace invites, critical tickets)
- **normal** - Standard notifications (most notifications)
- **low** - Low priority (digest emails, summaries)

## Status Flow

1. **pending** - Notification created, waiting to be sent
2. **sent** - Successfully delivered
3. **failed** - Delivery failed (with failure_reason)
4. **cancelled** - Notification cancelled before sending

## Migration Path for Existing Email Code

To migrate existing email sending code:

### Before:
```csharp
await _emailSender.SendAsync(email, subject, body);
```

### After:
```csharp
await _notificationService.CreateAsync(
    userId: userId,
    type: "workspace_invite",
    subject: subject,
    body: body,
    deliveryMethod: "email",
    workspaceId: workspaceId,
    priority: "high",
    createdBy: currentUserId
);
```

## Batch Processing

For digest emails or bulk notifications:
```csharp
await _notificationService.CreateBatchAsync(
    userIds: userIds,
    type: "ticket_summary",
    subject: "Daily Ticket Summary",
    body: summaryHtml,
    deliveryMethod: "email",
    workspaceId: workspaceId,
    priority: "low"
);
```

## Background Processing

To process pending notifications:
```csharp
// In a background service or scheduled job
await _notificationService.SendPendingEmailsAsync(batchSize: 100);
await _notificationService.SendPendingInAppAsync(batchSize: 100);
```

## Benefits

1. **Unified System**: Single table for all notification types
2. **Flexible Delivery**: Support for multiple delivery methods
3. **Batch Support**: Efficient bulk notifications
4. **Priority Queuing**: Urgent notifications processed first
5. **Failure Tracking**: Detailed error logging
6. **Read Receipts**: Track when users read in-app notifications
7. **Scheduled Delivery**: Send notifications at specific times
8. **Audit Trail**: Complete history of all notifications

## Next Steps

1. Update existing email sending code in:
   - `Tickflo.Web/Pages/Workspaces/UsersInvite.cshtml.cs`
   - `Tickflo.Web/Pages/Workspaces/Users.cshtml.cs`
   
2. Create background service for processing pending notifications

3. Build in-app notification UI component

4. Add notification preferences per user

5. Implement SMS and push notification providers
