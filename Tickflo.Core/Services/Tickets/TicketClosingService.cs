using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles the business workflow of closing and resolving tickets.
/// </summary>
public class TicketClosingService : ITicketClosingService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly ITicketStatusRepository _statusRepo;

    public TicketClosingService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo,
        ITicketStatusRepository statusRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
        _statusRepo = statusRepo;
    }

    /// <summary>
    /// Closes a ticket with a resolution note.
    /// </summary>
    public async Task<Ticket> CloseTicketAsync(
        int workspaceId, 
        int ticketId, 
        string resolutionNote, 
        int closedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Business rule: Cannot close an already closed ticket
        if (IsTicketClosed(ticket.Status))
            throw new InvalidOperationException($"Ticket is already closed with status '{ticket.Status}'");

        // Business rule: Resolution note is required when closing
        if (string.IsNullOrWhiteSpace(resolutionNote))
            throw new InvalidOperationException("Resolution note is required when closing a ticket");

        var previousStatus = ticket.Status;

        // Set to closed status
        ticket.Status = "Closed";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        // Log the closure
        await _historyRepo.CreateAsync(new TicketHistory
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
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Business rule: Can only reopen closed tickets
        if (!IsTicketClosed(ticket.Status))
            throw new InvalidOperationException("Can only reopen closed tickets");

        // Business rule: Reason is required for reopening
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Reason is required when reopening a ticket");

        var previousStatus = ticket.Status;

        // Reopen to a working status
        ticket.Status = "Open";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        // Log the reopening
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = reopenedByUserId,
            Action = "reopened",
            Note = $"Ticket reopened from '{previousStatus}'. Reason: {reason}"
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
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        if (IsTicketClosed(ticket.Status))
            throw new InvalidOperationException("Cannot resolve a closed ticket");

        if (string.IsNullOrWhiteSpace(resolutionNote))
            throw new InvalidOperationException("Resolution note is required");

        var previousStatus = ticket.Status;

        ticket.Status = "Resolved";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        await _historyRepo.CreateAsync(new TicketHistory
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
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        if (IsTicketClosed(ticket.Status))
            throw new InvalidOperationException("Ticket is already closed");

        if (string.IsNullOrWhiteSpace(cancellationReason))
            throw new InvalidOperationException("Cancellation reason is required");

        var previousStatus = ticket.Status;

        ticket.Status = "Cancelled";
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = cancelledByUserId,
            Action = "cancelled",
            Note = $"Ticket cancelled. Reason: {cancellationReason}"
        });

        return ticket;
    }

    private static bool IsTicketClosed(string status)
    {
        return status.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
               status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
    }
}
