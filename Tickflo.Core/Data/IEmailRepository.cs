namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

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
