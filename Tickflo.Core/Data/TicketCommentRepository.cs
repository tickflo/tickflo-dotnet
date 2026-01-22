namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

/// <summary>
/// Data access implementation for ticket comment persistence.
/// Provides workspace-scoped CRUD operations with user audit trail support.
/// </summary>
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
