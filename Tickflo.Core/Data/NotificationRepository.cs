namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class NotificationRepository(TickfloDbContext db) : INotificationRepository
{
    private readonly TickfloDbContext _db = db;

    public async Task<Notification?> FindByIdAsync(int id) => await this._db.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public async Task<List<Notification>> ListForUserAsync(int userId, bool unreadOnly = false)
    {
        var query = this._db.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> ListPendingAsync(string deliveryMethod, int limit = 100) => await this._db.Notifications
            .Where(n => n.Status == "pending" && n.DeliveryMethod == deliveryMethod)
            .Where(n => n.ScheduledFor == null || n.ScheduledFor <= DateTime.UtcNow)
            .OrderBy(n => n.Priority == "urgent" ? 0 : n.Priority == "high" ? 1 : n.Priority == "normal" ? 2 : 3)
            .ThenBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();

    public async Task<List<Notification>> ListByBatchIdAsync(string batchId) => await this._db.Notifications
            .Where(n => n.BatchId == batchId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Notification notification)
    {
        this._db.Notifications.Add(notification);
        await this._db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        this._db.Notifications.Update(notification);
        await this._db.SaveChangesAsync();
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

    public async Task<int> CountUnreadForUserAsync(int userId) => await this._db.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .CountAsync();
}
