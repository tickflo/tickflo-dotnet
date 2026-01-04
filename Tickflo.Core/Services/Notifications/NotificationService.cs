using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;

namespace Tickflo.Core.Services.Notifications;

public interface INotificationService
{
    Task CreateAsync(int userId, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null, string? data = null);
    Task CreateBatchAsync(List<int> userIds, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null);
    Task SendPendingEmailsAsync(int batchSize = 100);
    Task SendPendingInAppAsync(int batchSize = 100);
}

public class NotificationService(INotificationRepository notifications, IEmailSender emailSender) : INotificationService
{
    private readonly INotificationRepository _notifications = notifications;
    private readonly IEmailSender _emailSender = emailSender;

    public async Task CreateAsync(int userId, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null, string? data = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = type,
            DeliveryMethod = deliveryMethod,
            Priority = priority,
            Subject = subject,
            Body = body,
            Data = data,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        await _notifications.AddAsync(notification);
    }

    public async Task CreateBatchAsync(List<int> userIds, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null)
    {
        var batchId = Guid.NewGuid().ToString();
        
        foreach (var userId in userIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                Type = type,
                DeliveryMethod = deliveryMethod,
                Priority = priority,
                Subject = subject,
                Body = body,
                Status = "pending",
                BatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await _notifications.AddAsync(notification);
        }
    }

    public async Task SendPendingEmailsAsync(int batchSize = 100)
    {
        var pending = await _notifications.ListPendingAsync("email", batchSize);

        foreach (var notification in pending)
        {
            try
            {
                // Get user email - you'll need to inject IUserRepository
                // For now, assuming the email is in the notification data or we need to look it up
                var toEmail = notification.Data ?? ""; // This should be properly resolved from user
                
                await _emailSender.SendAsync(toEmail, notification.Subject, notification.Body);
                await _notifications.MarkAsSentAsync(notification.Id);
            }
            catch (Exception ex)
            {
                await _notifications.MarkAsFailedAsync(notification.Id, ex.Message);
            }
        }
    }

    public async Task SendPendingInAppAsync(int batchSize = 100)
    {
        var pending = await _notifications.ListPendingAsync("in_app", batchSize);

        foreach (var notification in pending)
        {
            // In-app notifications are just marked as sent since they're already in the database
            // The UI will query them directly
            await _notifications.MarkAsSentAsync(notification.Id);
        }
    }
}
