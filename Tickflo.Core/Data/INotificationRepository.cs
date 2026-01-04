using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface INotificationRepository
{
    Task<Notification?> FindByIdAsync(int id);
    Task<List<Notification>> ListForUserAsync(int userId, bool unreadOnly = false);
    Task<List<Notification>> ListPendingAsync(string deliveryMethod, int limit = 100);
    Task<List<Notification>> ListByBatchIdAsync(string batchId);
    Task AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task MarkAsReadAsync(int id);
    Task MarkAsSentAsync(int id);
    Task MarkAsFailedAsync(int id, string reason);
    Task<int> CountUnreadForUserAsync(int userId);
}
