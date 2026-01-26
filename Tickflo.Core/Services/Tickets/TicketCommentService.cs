namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business logic for managing ticket comments with dual visibility support.
/// Supports both internal-only and client-visible comments with full audit trails.
/// </summary>

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
    public Task<IReadOnlyList<TicketComment>> GetCommentsAsync(int workspaceId, int ticketId, bool isClientView = false, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment on a ticket with specified visibility and audit trail.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping</param>
    /// <param name="ticketId">The ticket ID to add the comment to</param>
    /// <param name="createdByUserId">The user ID creating the comment (for audit trail)</param>
    /// <param name="content">The comment text (must be non-empty)</param>
    /// <param name="isVisibleToClient">If true, comment is visible to clients; if false, internal-only</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The newly created comment with assigned ID and timestamps</returns>
    /// <exception cref="InvalidOperationException">Thrown if content is empty or null</exception>
    public Task<TicketComment> AddCommentAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default);

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
    public Task<TicketComment> UpdateCommentAsync(int workspaceId, int commentId, string content, int updatedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new comment on a ticket from a client with specified visibility.
    /// Automatically marks the comment as visible to client since it's submitted by the client.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping</param>
    /// <param name="ticketId">The ticket ID to add the comment to</param>
    /// <param name="contactId">The contact ID of the client creating the comment</param>
    /// <param name="content">The comment text (must be non-empty)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The newly created comment with assigned ID and timestamps</returns>
    /// <exception cref="InvalidOperationException">Thrown if content is empty or null</exception>
    public Task<TicketComment> AddClientCommentAsync(int workspaceId, int ticketId, int contactId, string content, CancellationToken ct = default);

    /// <summary>
    /// Deletes a comment from a ticket. This is a hard delete operation.
    /// </summary>
    /// <param name="workspaceId">The workspace ID for scoping and security</param>
    /// <param name="commentId">The comment ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <remarks>If the comment does not exist, the operation completes silently (idempotent)</remarks>
    public Task DeleteCommentAsync(int workspaceId, int commentId, CancellationToken ct = default);
}

public class TicketCommentService(ITicketCommentRepository commentRepo) : ITicketCommentService
{
    /// <summary>
    /// Retrieves comments for a ticket with visibility filtering based on view context.
    /// </summary>
    public async Task<IReadOnlyList<TicketComment>> GetCommentsAsync(int workspaceId, int ticketId, bool isClientView = false, CancellationToken ct = default)
    {
        // Business rule: Workspace and ticket IDs must be positive
        ValidateIdentifiers(workspaceId, ticketId);

        var comments = await commentRepo.ListByTicketAsync(workspaceId, ticketId, ct);

        // Business rule: Client view shows only comments marked as client-visible
        if (isClientView)
        {
            return [.. comments.Where(c => c.IsVisibleToClient)];
        }

        // Internal view shows all comments (no filtering)
        return comments;
    }

    /// <summary>
    /// Creates a new comment with content validation and audit trail setup.
    /// </summary>
    public async Task<TicketComment> AddCommentAsync(int workspaceId, int ticketId, int createdByUserId, string content, bool isVisibleToClient, CancellationToken ct = default)
    {
        // Business rule: All identifiers must be positive
        ValidateIdentifiers(workspaceId, ticketId);
        if (createdByUserId <= 0)
        {
            throw new InvalidOperationException("Invalid user ID for comment creator");
        }

        // Business rule: Comment content is required and must be non-empty
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Comment content cannot be empty");
        }

        var comment = new TicketComment
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = createdByUserId,
            Content = content.Trim(),
            IsVisibleToClient = isVisibleToClient,
        };

        return await commentRepo.CreateAsync(comment, ct);
    }

    /// <summary>
    /// Creates a new comment from a client on a ticket.
    /// Automatically marks as visible to client and records the contact ID.
    /// </summary>
    public async Task<TicketComment> AddClientCommentAsync(int workspaceId, int ticketId, int contactId, string content, CancellationToken ct = default)
    {
        // Business rule: All identifiers must be positive
        ValidateIdentifiers(workspaceId, ticketId);
        if (contactId <= 0)
        {
            throw new InvalidOperationException("Invalid contact ID for comment creator");
        }

        // Business rule: Comment content is required and must be non-empty
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Comment content cannot be empty");
        }

        // Use a system user ID (1) for client comments, track actual contact via CreatedByContactId
        var comment = new TicketComment
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = 1, // System user ID for client comments
            CreatedByContactId = contactId, // Track actual client
            Content = content.Trim(),
            IsVisibleToClient = true, // Client comments are always visible to client
        };

        return await commentRepo.CreateAsync(comment, ct);
    }

    /// <summary>
    /// Updates comment content with new audit trail metadata.
    /// </summary>
    public async Task<TicketComment> UpdateCommentAsync(int workspaceId, int commentId, string content, int updatedByUserId, CancellationToken ct = default)
    {
        // Business rule: All identifiers must be positive
        ValidateIdentifiers(workspaceId, commentId);
        if (updatedByUserId <= 0)
        {
            throw new InvalidOperationException("Invalid user ID for comment updater");
        }

        // Business rule: Updated content is required and must be non-empty
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Comment content cannot be empty");
        }

        var comment = await commentRepo.FindAsync(workspaceId, commentId, ct) ?? throw new InvalidOperationException($"Comment {commentId} not found in workspace {workspaceId}");

        // Update comment with new content and audit metadata
        comment.Content = content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        comment.UpdatedByUserId = updatedByUserId;

        return await commentRepo.UpdateAsync(comment, ct);
    }

    /// <summary>
    /// Deletes a comment from the system.
    /// Operation is idempotent - if comment doesn't exist, no error is thrown.
    /// </summary>
    public async Task DeleteCommentAsync(int workspaceId, int commentId, CancellationToken ct = default)
    {
        // Business rule: All identifiers must be positive
        ValidateIdentifiers(workspaceId, commentId);

        await commentRepo.DeleteAsync(workspaceId, commentId, ct);
    }

    /// <summary>
    /// Validates that workspace and entity IDs are positive values.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if either ID is not positive</exception>
    private static void ValidateIdentifiers(int workspaceId, int entityId)
    {
        if (workspaceId <= 0)
        {
            throw new InvalidOperationException("Invalid workspace ID");
        }

        if (entityId <= 0)
        {
            throw new InvalidOperationException("Invalid entity ID");
        }
    }
}
