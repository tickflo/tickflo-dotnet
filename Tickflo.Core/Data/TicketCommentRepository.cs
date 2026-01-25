namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

/// <summary>
/// Data access implementation for ticket comment persistence.
/// Provides workspace-scoped CRUD operations with user audit trail support.
/// </summary>

/// <summary>
/// Data access interface for ticket comment persistence operations.
/// Supports CRUD operations with workspace scoping for security.
/// </summary>
public interface ITicketCommentRepository
{
    /// <summary>
    /// Retrieves all comments for a specific ticket, ordered by creation date.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping</param>
    /// <param name="ticketId">The ticket ID to retrieve comments for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Read-only list of comments ordered by creation date ascending</returns>
    public Task<IReadOnlyList<TicketComment>> ListByTicketAsync(int workspaceId, int ticketId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single comment by ID with user information included.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The comment if found and in the specified workspace; null otherwise</returns>
    public Task<TicketComment?> FindAsync(int workspaceId, int commentId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment and assigns an ID.
    /// </summary>
    /// <param name="comment">The comment entity to create (ID should be 0 for new records)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created comment with assigned ID and timestamps</returns>
    public Task<TicketComment> CreateAsync(TicketComment comment, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing comment.
    /// </summary>
    /// <param name="comment">The comment entity with updated content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The updated comment</returns>
    public Task<TicketComment> UpdateAsync(TicketComment comment, CancellationToken ct = default);

    /// <summary>
    /// Deletes a comment from the database.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>If comment does not exist, operation completes without error (idempotent)</remarks>
    public Task DeleteAsync(int workspaceId, int commentId, CancellationToken ct = default);
}

public class TicketCommentRepository(TickfloDbContext dbContext) : ITicketCommentRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    /// <summary>
    /// Retrieves all comments for a ticket, ordered by creation date.
    /// Includes user information for comment authors.
    /// </summary>
    public async Task<IReadOnlyList<TicketComment>> ListByTicketAsync(int workspaceId, int ticketId, CancellationToken ct = default)
        => await this.dbContext.TicketComments
            .Where(c => c.WorkspaceId == workspaceId && c.TicketId == ticketId)
            .Include(c => c.CreatedByUser)
            .Include(c => c.UpdatedByUser)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Retrieves a single comment by ID within a specific workspace.
    /// Includes user information for creators and editors.
    /// </summary>
    public async Task<TicketComment?> FindAsync(int workspaceId, int commentId, CancellationToken ct = default)
        => await this.dbContext.TicketComments
            .Where(c => c.WorkspaceId == workspaceId && c.Id == commentId)
            .Include(c => c.CreatedByUser)
            .Include(c => c.UpdatedByUser)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Creates a new comment in the database and returns it with assigned ID.
    /// </summary>
    public async Task<TicketComment> CreateAsync(TicketComment comment, CancellationToken ct = default)
    {
        this.dbContext.TicketComments.Add(comment);
        await this.dbContext.SaveChangesAsync(ct);
        return comment;
    }

    /// <summary>
    /// Updates an existing comment in the database.
    /// </summary>
    public async Task<TicketComment> UpdateAsync(TicketComment comment, CancellationToken ct = default)
    {
        this.dbContext.TicketComments.Update(comment);
        await this.dbContext.SaveChangesAsync(ct);
        return comment;
    }

    /// <summary>
    /// Deletes a comment from the database by ID within a workspace.
    /// Workspace scoping ensures users cannot delete comments from other workspaces.
    /// </summary>
    public async Task DeleteAsync(int workspaceId, int commentId, CancellationToken ct = default)
    {
        var comment = await this.dbContext.TicketComments.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == commentId, ct);
        if (comment != null)
        {
            this.dbContext.TicketComments.Remove(comment);
            await this.dbContext.SaveChangesAsync(ct);
        }
    }
}
