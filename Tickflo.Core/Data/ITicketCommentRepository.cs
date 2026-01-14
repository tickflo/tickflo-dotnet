using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

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
    Task<IReadOnlyList<TicketComment>> ListByTicketAsync(int workspaceId, int ticketId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single comment by ID with user information included.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The comment if found and in the specified workspace; null otherwise</returns>
    Task<TicketComment?> FindAsync(int workspaceId, int commentId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment and assigns an ID.
    /// </summary>
    /// <param name="comment">The comment entity to create (ID should be 0 for new records)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created comment with assigned ID and timestamps</returns>
    Task<TicketComment> CreateAsync(TicketComment comment, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing comment.
    /// </summary>
    /// <param name="comment">The comment entity with updated content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The updated comment</returns>
    Task<TicketComment> UpdateAsync(TicketComment comment, CancellationToken ct = default);

    /// <summary>
    /// Deletes a comment from the database.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>If comment does not exist, operation completes without error (idempotent)</remarks>
    Task DeleteAsync(int workspaceId, int commentId, CancellationToken ct = default);
}
