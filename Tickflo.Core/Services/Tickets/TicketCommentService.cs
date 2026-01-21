namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business logic for managing ticket comments with dual visibility support.
/// Supports both internal-only and client-visible comments with full audit trails.
/// </summary>
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
            return comments.Where(c => c.IsVisibleToClient).ToList();
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
            CreatedAt = DateTime.UtcNow
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
            CreatedAt = DateTime.UtcNow
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
