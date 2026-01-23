namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

/// <summary>
/// Repository implementation for managing email records.
/// </summary>
public class EmailRepository(TickfloDbContext dbContext) : IEmailRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<Email> AddAsync(Email email)
    {
        this.dbContext.Emails.Add(email);
        await this.dbContext.SaveChangesAsync();
        return email;
    }

    public async Task<Email?> FindByIdAsync(int id)
    {
        return await this.dbContext.Emails.FindAsync(id);
    }
}
