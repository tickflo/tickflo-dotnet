namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

/// <summary>
/// Repository implementation for managing email records.
/// </summary>

/// <summary>
/// Repository for managing email records.
/// </summary>
public interface IEmailRepository
{
    /// <summary>
    /// Adds a new email record to the database.
    /// </summary>
    /// <param name="email">Email entity to add</param>
    /// <returns>The added email with its generated ID</returns>
    public Task<Email> AddAsync(Email email);

    /// <summary>
    /// Finds an email by its ID.
    /// </summary>
    /// <param name="id">Email ID</param>
    /// <returns>Email entity or null if not found</returns>
    public Task<Email?> FindByIdAsync(int id);
}

public class EmailRepository(TickfloDbContext dbContext) : IEmailRepository
{
    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<Email> AddAsync(Email email)
    {
        this.dbContext.Emails.Add(email);
        await this.dbContext.SaveChangesAsync();
        return email;
    }

    public async Task<Email?> FindByIdAsync(int id) => await this.dbContext.Emails.FindAsync(id);
}
