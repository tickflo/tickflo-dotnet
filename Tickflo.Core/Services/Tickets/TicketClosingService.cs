namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of closing and resolving tickets.
/// </summary>

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

public class TicketClosingService(
    ITicketRepository ticketRepository,
    ITicketHistoryRepository historyRepository,
    ITicketStatusRepository statusRepository) : ITicketClosingService
{
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketHistoryRepository historyRepository = historyRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;

    /// <summary>
    /// Closes a ticket with a resolution note.
    /// </summary>
    public async Task<Ticket> CloseTicketAsync(
        int workspaceId,
        int ticketId,
        string resolutionNote,
        int closedByUserId)
    {
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        // Resolve closed status ID
        var closedStatus = await this.statusRepository.FindByIsClosedStateAsync(workspaceId, true) ?? throw new InvalidOperationException("Closed status not found in workspace");

        // Business rule: Cannot close an already closed ticket
        if (ticket.StatusId == closedStatus.Id)
        {
            throw new InvalidOperationException("Ticket is already closed");
        }

        // Business rule: Resolution note is required when closing
        if (string.IsNullOrWhiteSpace(resolutionNote))
        {
            throw new InvalidOperationException("Resolution note is required when closing a ticket");
        }

        ticket.StatusId = closedStatus.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        // Log the closure
        await this.historyRepository.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = closedByUserId,
            Action = "closed",
            Note = $"Ticket closed. Resolution: {resolutionNote}"
        });

        // Could add: Send notifications, update SLA metrics, trigger surveys, etc.

        return ticket;
    }

    /// <summary>
    /// Reopens a previously closed ticket.
    /// </summary>
    public async Task<Ticket> ReopenTicketAsync(
        int workspaceId,
        int ticketId,
        string reason,
        int reopenedByUserId)
    {
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        var closedStatus = await this.statusRepository.FindByIsClosedStateAsync(workspaceId, true) ?? throw new InvalidOperationException("Closed status not found in workspace");

        // Business rule: Can only reopen closed tickets
        if (ticket.StatusId != closedStatus.Id)
        {
            throw new InvalidOperationException("Can only reopen closed tickets");
        }

        // Business rule: Reason is required for reopening
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Reason is required when reopening a ticket");
        }

        var openStatus = await this.statusRepository.FindByNameAsync(workspaceId, "Open") ?? throw new InvalidOperationException("'Open' status not found in workspace");

        ticket.StatusId = openStatus.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        // Log the reopening
        await this.historyRepository.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = reopenedByUserId,
            Action = "reopened",
            Note = $"Ticket reopened. Reason: {reason}"
        });

        // Could add: Notify original assignee, reset SLA timers, etc.

        return ticket;
    }

    /// <summary>
    /// Marks a ticket as resolved (awaiting closure confirmation).
    /// </summary>
    public async Task<Ticket> ResolveTicketAsync(
        int workspaceId,
        int ticketId,
        string resolutionNote,
        int resolvedByUserId)
    {
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        var closedStatus = await this.statusRepository.FindByIsClosedStateAsync(workspaceId, true);
        var resolvedStatus = await this.statusRepository.FindByNameAsync(workspaceId, "Resolved") ?? throw new InvalidOperationException("'Resolved' status not found in workspace");

        if (closedStatus != null && ticket.StatusId == closedStatus.Id)
        {
            throw new InvalidOperationException("Cannot resolve a closed ticket");
        }

        if (string.IsNullOrWhiteSpace(resolutionNote))
        {
            throw new InvalidOperationException("Resolution note is required");
        }

        ticket.StatusId = resolvedStatus.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        await this.historyRepository.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = resolvedByUserId,
            Action = "resolved",
            Note = $"Ticket resolved. {resolutionNote}"
        });

        // Could add: Start auto-close timer, request feedback, etc.

        return ticket;
    }

    /// <summary>
    /// Cancels a ticket without resolution.
    /// </summary>
    public async Task<Ticket> CancelTicketAsync(
        int workspaceId,
        int ticketId,
        string cancellationReason,
        int cancelledByUserId)
    {
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException("Ticket not found");

        var closedStatus = await this.statusRepository.FindByIsClosedStateAsync(workspaceId, true);
        var cancelledStatus = await this.statusRepository.FindByNameAsync(workspaceId, "Cancelled") ?? throw new InvalidOperationException("'Cancelled' status not found in workspace");

        if (closedStatus != null && ticket.StatusId == closedStatus.Id)
        {
            throw new InvalidOperationException("Ticket is already closed");
        }

        if (string.IsNullOrWhiteSpace(cancellationReason))
        {
            throw new InvalidOperationException("Cancellation reason is required");
        }

        ticket.StatusId = cancelledStatus.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);

        await this.historyRepository.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = cancelledByUserId,
            Action = "cancelled",
            Note = $"Ticket cancelled. Reason: {cancellationReason}"
        });

        return ticket;
    }
}
