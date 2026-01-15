using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public interface IContactRepository
{
    Task<IReadOnlyList<Contact>> ListAsync(int workspaceId, CancellationToken ct = default);
    Task<Contact?> FindAsync(int workspaceId, int id, CancellationToken ct = default);
    Task<Contact> CreateAsync(Contact contact, CancellationToken ct = default);
    Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default);
    Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default);
}