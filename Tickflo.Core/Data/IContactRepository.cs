namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

public interface IContactRepository
{
    public Task<IReadOnlyList<Contact>> ListAsync(int workspaceId, CancellationToken ct = default);
    public Task<Contact?> FindAsync(int workspaceId, int id, CancellationToken ct = default);
    public Task<Contact> CreateAsync(Contact contact, CancellationToken ct = default);
    public Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default);
    public Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}
