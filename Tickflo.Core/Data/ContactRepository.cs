namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class ContactRepository(TickfloDbContext dbContext) : IContactRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<IReadOnlyList<Contact>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await this.dbContext.Contacts.Where(c => c.WorkspaceId == workspaceId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Contact?> FindAsync(int workspaceId, int id, CancellationToken ct = default)
        => await this.dbContext.Contacts.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == id, ct);

    public async Task<Contact> CreateAsync(Contact contact, CancellationToken ct = default)
    {
        this.dbContext.Contacts.Add(contact);
        await this.dbContext.SaveChangesAsync(ct);
        return contact;
    }

    public async Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default)
    {
        this.dbContext.Contacts.Update(contact);
        await this.dbContext.SaveChangesAsync(ct);
        return contact;
    }

    public async Task DeleteAsync(int workspaceId, int id, CancellationToken ct = default)
    {
        var entity = await this.FindAsync(workspaceId, id, ct);
        if (entity is null)
        {
            return;
        }

        this.dbContext.Contacts.Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);
    }
}
