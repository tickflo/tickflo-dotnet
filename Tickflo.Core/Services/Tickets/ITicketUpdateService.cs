using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles ticket update workflows.
/// </summary>
public interface ITicketUpdateService
{
    /// <summary>
    /// Updates ticket core information with change tracking.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to update</param>
    /// <param name="request">Update details</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> UpdateTicketInfoAsync(int workspaceId, int ticketId, TicketUpdateRequest request, int updatedByUserId);

    /// <summary>
    /// Updates ticket priority with audit trail.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to update</param>
    /// <param name="newPriority">New priority level</param>
    /// <param name="reason">Optional reason for change</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> UpdatePriorityAsync(int workspaceId, int ticketId, string newPriority, string? reason, int updatedByUserId);

    /// <summary>
    /// Updates ticket status with transition validation.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to update</param>
    /// <param name="newStatus">New status</param>
    /// <param name="reason">Optional reason for change</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated ticket</returns>
    Task<Ticket> UpdateStatusAsync(int workspaceId, int ticketId, string newStatus, string? reason, int updatedByUserId);

    /// <summary>
    /// Adds a note to a ticket.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket to add note to</param>
    /// <param name="note">Note content</param>
    /// <param name="addedByUserId">User adding the note</param>
    /// <returns>The ticket</returns>
    Task<Ticket> AddNoteAsync(int workspaceId, int ticketId, string note, int addedByUserId);
}
