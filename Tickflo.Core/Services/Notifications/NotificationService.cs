namespace Tickflo.Core.Services.Notifications;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface INotificationService
{
    public Task CreateAsync(int userId, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null, string? data = null);
    public Task CreateBatchAsync(List<int> userIds, string type, string subject, string body, string deliveryMethod = "email", int? workspaceId = null, string priority = "normal", int? createdBy = null);
    public Task SendPendingEmailsAsync(int batchSize = 100);
    public Task SendPendingInAppAsync(int batchSize = 100);
}

public class NotificationService(INotificationRepository notificationRepository) : INotificationService
{
    private readonly INotificationRepository notificationRepository = notificationRepository;

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

        await this.notificationRepository.AddAsync(notification);
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

            await this.notificationRepository.AddAsync(notification);
        }
    }

    public async Task SendPendingEmailsAsync(int batchSize = 100)
    {
        var pending = await this.notificationRepository.ListPendingAsync("email", batchSize);

        foreach (var notification in pending)
        {
            try
            {
                // Get user email - you'll need to inject IUserRepository
                // For now, assuming the email is in the notification data or we need to look it up
                //var toEmail = notification.Data ?? ""; // This should be properly resolved from user

                //await this._emailSender.SendAsync(toEmail, notification.Subject, notification.Body);
                await this.notificationRepository.MarkAsSentAsync(notification.Id);
            }
            catch (Exception ex)
            {
                await this.notificationRepository.MarkAsFailedAsync(notification.Id, ex.Message);
            }
        }
    }

    public async Task SendPendingInAppAsync(int batchSize = 100)
    {
        var pending = await this.notificationRepository.ListPendingAsync("in_app", batchSize);

        foreach (var notification in pending)
        {
            // In-app notifications are just marked as sent since they're already in the database
            // The UI will query them directly
            await this.notificationRepository.MarkAsSentAsync(notification.Id);
        }
    }
}
