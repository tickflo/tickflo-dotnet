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
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;

    // Backward-compatible constructor
    public TicketUpdateService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo)
        : this(ticketRepo, historyRepo, statusRepo: null!, priorityRepo: null!)
    { }

    public TicketUpdateService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
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

        // Resolve PriorityId
        var pr = await _priorityRepo.FindAsync(workspaceId, newPriority.Trim());
        if (pr == null)
            throw new InvalidOperationException($"Priority '{newPriority}' not found in workspace");
        
        ticket.PriorityId = pr.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        var note = $"Priority changed to '{pr.Name}'";
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

        // Resolve StatusId and verify it exists
        var st = await _statusRepo.FindByNameAsync(workspaceId, newStatus.Trim());
        if (st == null)
            throw new InvalidOperationException($"Status '{newStatus}' not found in workspace");
        
        ticket.StatusId = st.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);

        var note = $"Status changed to '{st.Name}'";
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
        // Status transitions are now validated at the database level via foreign keys
        // and at the service layer via FindByNameAsync; remove hardcoded rules
        return true;
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
