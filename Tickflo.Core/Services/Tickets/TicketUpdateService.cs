using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles the business workflow of updating ticket information and tracking changes.
/// </summary>
public class TicketUpdateService : ITicketUpdateService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;

    public TicketUpdateService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
    }

    /// <summary>
    /// Updates ticket core information (subject, description, etc.).
    /// </summary>
    public async Task<Ticket> UpdateTicketInfoAsync(
        int workspaceId, 
        int ticketId, 
        TicketUpdateRequest request, 
        int updatedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        var changes = new List<string>();

        // Track changes for audit trail
        if (!string.IsNullOrWhiteSpace(request.Subject) && ticket.Subject != request.Subject.Trim())
        {
            changes.Add($"Subject changed from '{ticket.Subject}' to '{request.Subject.Trim()}'");
            ticket.Subject = request.Subject.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && ticket.Description != request.Description.Trim())
        {
            changes.Add("Description updated");
            ticket.Description = request.Description.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Type) && ticket.Type != request.Type.Trim())
        {
            changes.Add($"Type changed from '{ticket.Type}' to '{request.Type.Trim()}'");
            ticket.Type = request.Type.Trim();
        }

        if (request.ContactId.HasValue && ticket.ContactId != request.ContactId.Value)
        {
            changes.Add($"Contact changed from {ticket.ContactId} to {request.ContactId.Value}");
            ticket.ContactId = request.ContactId.Value;
        }

        if (request.LocationId.HasValue && ticket.LocationId != request.LocationId.Value)
        {
            changes.Add($"Location changed from {ticket.LocationId} to {request.LocationId.Value}");
            ticket.LocationId = request.LocationId.Value;
        }

        if (changes.Any())
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            await _ticketRepo.UpdateAsync(ticket);

            // Log all changes
            await _historyRepo.CreateAsync(new TicketHistory
            {
                WorkspaceId = workspaceId,
                TicketId = ticketId,
                CreatedByUserId = updatedByUserId,
                Action = "updated",
                Note = string.Join("; ", changes)
            });
        }

        return ticket;
    }

    /// <summary>
    /// Updates ticket priority.
    /// </summary>
    public async Task<Ticket> UpdatePriorityAsync(
        int workspaceId, 
        int ticketId, 
        string newPriority, 
        string? reason, 
        int updatedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        if (string.IsNullOrWhiteSpace(newPriority))
            throw new InvalidOperationException("Priority cannot be empty");

        var oldPriority = ticket.Priority;
        ticket.Priority = newPriority.Trim();
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        var note = $"Priority changed from '{oldPriority}' to '{ticket.Priority}'";
        if (!string.IsNullOrWhiteSpace(reason))
            note += $". Reason: {reason}";

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = updatedByUserId,
            Action = "priority_changed",
            Note = note
        });

        return ticket;
    }

    /// <summary>
    /// Updates ticket status with validation.
    /// </summary>
    public async Task<Ticket> UpdateStatusAsync(
        int workspaceId, 
        int ticketId, 
        string newStatus, 
        string? reason, 
        int updatedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        if (string.IsNullOrWhiteSpace(newStatus))
            throw new InvalidOperationException("Status cannot be empty");

        // Business rule: Prevent invalid status transitions
        if (!IsValidStatusTransition(ticket.Status, newStatus))
            throw new InvalidOperationException($"Cannot transition from '{ticket.Status}' to '{newStatus}'");

        var oldStatus = ticket.Status;
        ticket.Status = newStatus.Trim();
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        var note = $"Status changed from '{oldStatus}' to '{ticket.Status}'";
        if (!string.IsNullOrWhiteSpace(reason))
            note += $". Reason: {reason}";

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = updatedByUserId,
            Action = "status_changed",
            Note = note
        });

        return ticket;
    }

    /// <summary>
    /// Adds a note/comment to a ticket.
    /// </summary>
    public async Task<Ticket> AddNoteAsync(
        int workspaceId, 
        int ticketId, 
        string note, 
        int addedByUserId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        if (string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("Note cannot be empty");

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = addedByUserId,
            Action = "note_added",
            Note = note.Trim()
        });

        return ticket;
    }

    private static bool IsValidStatusTransition(string fromStatus, string toStatus)
    {
        // Define valid status transitions
        // This is a simple example - enhance based on business rules
        var validTransitions = new Dictionary<string, List<string>>
        {
            { "New", new List<string> { "Open", "Cancelled" } },
            { "Open", new List<string> { "In Progress", "Resolved", "Cancelled" } },
            { "In Progress", new List<string> { "Resolved", "On Hold", "Cancelled" } },
            { "On Hold", new List<string> { "Open", "Resolved", "Cancelled" } },
            { "Resolved", new List<string> { "Closed", "Reopened" } },
            { "Closed", new List<string> { "Reopened" } },
            { "Cancelled", new List<string> { "Reopened" } }
        };

        if (!validTransitions.TryGetValue(fromStatus, out var allowedTransitions))
            return false;

        return allowedTransitions.Contains(toStatus, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Request to update ticket information.
/// </summary>
public class TicketUpdateRequest
{
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int? ContactId { get; set; }
    public int? LocationId { get; set; }
}
