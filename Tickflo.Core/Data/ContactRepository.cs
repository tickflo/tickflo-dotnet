using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class ContactRepository(TickfloDbContext db) : IContactRepository
{
    public async Task<IReadOnlyList<Contact>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await db.Contacts.Where(c => c.WorkspaceId == workspaceId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Contact?> FindAsync(int workspaceId, int id, CancellationToken ct = default)
        => await db.Contacts.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == id, ct);

    public async Task<Contact?> FindByAccessTokenAsync(string token, CancellationToken ct = default)
        => await db.Contacts.FirstOrDefaultAsync(c => c.AccessToken == token, ct);

    public async Task<Contact> CreateAsync(Contact contact, CancellationToken ct = default)
    {
        db.Contacts.Add(contact);
        await db.SaveChangesAsync(ct);
        return contact;
    }

    public async Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default)
    {
        db.Contacts.Update(contact);
        await db.SaveChangesAsync(ct);
        return contact;
    }

    public async Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await FindAsync(workspaceId, id, ct);
        if (entity is null) return;
        db.Contacts.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}