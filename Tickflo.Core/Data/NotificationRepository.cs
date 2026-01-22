namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class NotificationRepository(TickfloDbContext dbContext) : INotificationRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<Notification?> FindByIdAsync(int id) => await this.dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public async Task<List<Notification>> ListForUserAsync(int userId, bool unreadOnly = false)
    {
        var query = this.dbContext.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> ListPendingAsync(string deliveryMethod, int limit = 100) => await this.dbContext.Notifications
            .Where(n => n.Status == "pending" && n.DeliveryMethod == deliveryMethod)
            .Where(n => n.ScheduledFor == null || n.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(n => n.Priority == "urgent" ? 0 : n.Priority == "high" ? 1 : n.Priority == "normal" ? 2 : 3)
            .ThenBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();

    public async Task<List<Notification>> ListByBatchIdAsync(string batchId) => await this.dbContext.Notifications
            .Where(n => n.BatchId == batchId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Notification notification)
    {
        this.dbContext.Notifications.Add(notification);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        this.dbContext.Notifications.Update(notification);
        await this.dbContext.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int id)
    {
        var notification = await this.FindByIdAsync(id);
        if (notification != null && notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await this.UpdateAsync(notification);
        }
    }

    public async Task MarkAsSentAsync(int id)
    {
        var notification = await this.FindByIdAsync(id);
        if (notification != null)
        {
            notification.Status = "sent";
            notification.SentAt = DateTime.UtcNow;
            await this.UpdateAsync(notification);
        }
    }

    public async Task MarkAsFailedAsync(int id, string reason)
    {
        var notification = await this.FindByIdAsync(id);
        if (notification != null)
        {
            notification.Status = "failed";
            notification.FailedAt = DateTime.UtcNow;
            notification.FailureReason = reason;
            await this.UpdateAsync(notification);
        }
    }

    public async Task<int> CountUnreadForUserAsync(int userId) => await this.dbContext.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .CountAsync();
}
