using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles ticket creation workflows.
/// </summary>
public interface ITicketCreationService
{
    /// <summary>
    /// Creates a new ticket with validation and auto-assignment.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Ticket creation details</param>
    /// <param name="createdByUserId">User creating the ticket</param>
    /// <returns>The created ticket</returns>
    Task<Ticket> CreateTicketAsync(int workspaceId, TicketCreationRequest request, int createdByUserId);

    /// <summary>
    /// Creates a ticket linked to a specific contact.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="contactId">Contact the ticket relates to</param>
    /// <param name="request">Ticket creation details</param>
    /// <param name="createdByUserId">User creating the ticket</param>
    /// <returns>The created ticket</returns>
    Task<Ticket> CreateFromContactAsync(int workspaceId, int contactId, TicketCreationRequest request, int createdByUserId);

    /// <summary>
    /// Bulk creates multiple tickets (e.g., from import).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="requests">Ticket creation requests</param>
    /// <param name="createdByUserId">User creating tickets</param>
    /// <returns>List of created tickets</returns>
    Task<List<Ticket>> CreateBulkAsync(int workspaceId, List<TicketCreationRequest> requests, int createdByUserId);
}
