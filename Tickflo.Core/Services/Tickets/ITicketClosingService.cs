namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Entities;

/// <summary>
/// Handles ticket closing and resolution workflows.
/// </summary>
public interface ITicketClosingService
{
    /// <summary>
    /// Closes a ticket with resolution note.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to close</param>
    /// <param name="resolutionNote">Resolution details</param>
    /// <param name="closedByUserId">User closing the ticket</param>
    /// <returns>The closed ticket</returns>
    public Task<Ticket> CloseTicketAsync(int workspaceId, int ticketId, string resolutionNote, int closedByUserId);

    /// <summary>
    /// Reopens a previously closed ticket.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to reopen</param>
    /// <param name="reason">Reason for reopening</param>
    /// <param name="reopenedByUserId">User reopening the ticket</param>
    /// <returns>The reopened ticket</returns>
    public Task<Ticket> ReopenTicketAsync(int workspaceId, int ticketId, string reason, int reopenedByUserId);

    /// <summary>
    /// Marks a ticket as resolved (awaiting confirmation).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to resolve</param>
    /// <param name="resolutionNote">Resolution details</param>
    /// <param name="resolvedByUserId">User resolving the ticket</param>
    /// <returns>The resolved ticket</returns>
    public Task<Ticket> ResolveTicketAsync(int workspaceId, int ticketId, string resolutionNote, int resolvedByUserId);

    /// <summary>
    /// Cancels a ticket without resolution.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to cancel</param>
    /// <param name="cancellationReason">Reason for cancellation</param>
    /// <param name="cancelledByUserId">User cancelling the ticket</param>
    /// <returns>The cancelled ticket</returns>
    public Task<Ticket> CancelTicketAsync(int workspaceId, int ticketId, string cancellationReason, int cancelledByUserId);
}
