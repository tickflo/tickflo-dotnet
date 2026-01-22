namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface INotificationRepository
{
    public Task<Notification?> FindByIdAsync(int id);
    public Task<List<Notification>> ListForUserAsync(int userId, bool unreadOnly = false);
    public Task<List<Notification>> ListPendingAsync(string deliveryMethod, int limit = 100);
    public Task<List<Notification>> ListByBatchIdAsync(string batchId);
    public Task AddAsync(Notification notification);
    public Task UpdateAsync(Notification notification);
    public Task MarkAsReadAsync(int id);
    public Task MarkAsSentAsync(int id);
    public Task MarkAsFailedAsync(int id, string reason);
    public Task<int> CountUnreadForUserAsync(int userId);
}
