using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Service for managing ticket comments with support for client-visible and internal-only comments.
/// Handles comment operations including creation, updates, retrieval, and deletion with proper access control.
/// </summary>
public interface ITicketCommentService
{
    /// <summary>
    /// Retrieves all comments for a given ticket, filtered by visibility based on the view context.
    /// Internal view returns all comments; client view returns only client-visible comments.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="ticketId">The ticket ID to retrieve comments for</param>
    /// <param name="isClientView">If true, filters to only client-visible comments; if false, returns all comments</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Read-only list of comments matching the visibility criteria, ordered by creation date</returns>
    Task<IReadOnlyList<TicketComment>> GetCommentsAsync(int workspaceId, int ticketId, bool isClientView = false, CancellationToken ct = default);
    
    /// <summary>
    /// Creates a new comment on a ticket with specified visibility and audit trail.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping</param>
    /// <param name="ticketId">The ticket ID to add the comment to</param>
    /// <param name="createdByUserId">The user ID creating the comment (for audit trail)</param>
    /// <param name="content">The comment text (must be non-empty)</param>
    /// <param name="isVisibleToClient">If true, comment is visible in client portal; if false, internal-only</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The newly created comment with assigned ID and timestamps</returns>
    /// <exception cref="InvalidOperationException">Thrown if content is empty or null</exception>
    Task<TicketComment> AddCommentAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default);
    
    /// <summary>
    /// Updates the content of an existing comment and records the update timestamp and user.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to update</param>
    /// <param name="content">The new comment content (must be non-empty)</param>
    /// <param name="updatedByUserId">The user ID performing the update (for audit trail)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The updated comment with new content and update metadata</returns>
    /// <exception cref="InvalidOperationException">Thrown if comment not found or content is empty</exception>
    Task<TicketComment> UpdateCommentAsync(int workspaceId, int commentId, string content, int updatedByUserId, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a comment from a ticket. This is a hard delete operation.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>If the comment does not exist, the operation completes silently (idempotent)</remarks>
    Task DeleteCommentAsync(int workspaceId, int commentId, CancellationToken ct = default);
}
