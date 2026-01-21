using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles the business workflow of updating ticket information and tracking changes.
/// </summary>
public class TicketUpdateService : ITicketUpdateService
{
    private const string ActionUpdated = "updated";
    private const string ActionPriorityChanged = "priority_changed";
    private const string ActionStatusChanged = "status_changed";
    private const string ActionNoteAdded = "note_added";
    
    private const string ErrorTicketNotFound = "Ticket not found";
    private const string ErrorPriorityEmpty = "Priority cannot be empty";
    private const string ErrorPriorityNotFound = "Priority '{0}' not found in workspace";
    private const string ErrorStatusEmpty = "Status cannot be empty";
    private const string ErrorStatusNotFound = "Status '{0}' not found in workspace";
    private const string ErrorNoteEmpty = "Note cannot be empty";

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
        var ticket = await GetTicketOrThrowAsync(workspaceId, ticketId);
        var changes = TrackTicketChanges(ticket, request);

        if (changes.Any())
        {
            ApplyTicketChanges(ticket, request);
            ticket.UpdatedAt = DateTime.UtcNow;
            await _ticketRepo.UpdateAsync(ticket);
            await LogChangesAsync(workspaceId, ticketId, updatedByUserId, changes);
        }

        return ticket;
    }

    private async Task<Ticket> GetTicketOrThrowAsync(int workspaceId, int ticketId)
    {
        var ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
        if (ticket == null)
            throw new InvalidOperationException(ErrorTicketNotFound);
        return ticket;
    }

    private static List<string> TrackTicketChanges(Ticket ticket, TicketUpdateRequest request)
    {
        var changes = new List<string>();

        if (ShouldUpdateSubject(ticket, request))
            changes.Add($"Subject changed from '{ticket.Subject}' to '{request.Subject!.Trim()}'");

        if (ShouldUpdateDescription(ticket, request))
            changes.Add("Description updated");

        if (ShouldUpdateContact(ticket, request))
            changes.Add($"Contact changed from {ticket.ContactId} to {request.ContactId}");

        if (ShouldUpdateLocation(ticket, request))
            changes.Add($"Location changed from {ticket.LocationId} to {request.LocationId}");

        return changes;
    }

    private static bool ShouldUpdateSubject(Ticket ticket, TicketUpdateRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Subject) && ticket.Subject != request.Subject.Trim();
    }

    private static bool ShouldUpdateDescription(Ticket ticket, TicketUpdateRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Description) && ticket.Description != request.Description.Trim();
    }

    private static bool ShouldUpdateContact(Ticket ticket, TicketUpdateRequest request)
    {
        return request.ContactId.HasValue && ticket.ContactId != request.ContactId.Value;
    }

    private static bool ShouldUpdateLocation(Ticket ticket, TicketUpdateRequest request)
    {
        return request.LocationId.HasValue && ticket.LocationId != request.LocationId.Value;
    }

    private static void ApplyTicketChanges(Ticket ticket, TicketUpdateRequest request)
    {
        if (ShouldUpdateSubject(ticket, request))
            ticket.Subject = request.Subject!.Trim();

        if (ShouldUpdateDescription(ticket, request))
            ticket.Description = request.Description!.Trim();

        if (ShouldUpdateContact(ticket, request))
            ticket.ContactId = request.ContactId!.Value;

        if (ShouldUpdateLocation(ticket, request))
            ticket.LocationId = request.LocationId!.Value;
    }

    private async Task LogChangesAsync(int workspaceId, int ticketId, int updatedByUserId, List<string> changes)
    {
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = updatedByUserId,
            Action = ActionUpdated,
            Note = string.Join("; ", changes)
        });
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
        var ticket = await GetTicketOrThrowAsync(workspaceId, ticketId);
        ValidateNotEmpty(newPriority, ErrorPriorityEmpty);

        var priority = await GetPriorityOrThrowAsync(workspaceId, newPriority);
        
        ticket.PriorityId = priority.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);
        await LogPriorityChangeAsync(workspaceId, ticketId, updatedByUserId, priority.Name, reason);

        return ticket;
    }

    private async Task<TicketPriority> GetPriorityOrThrowAsync(int workspaceId, string priorityName)
    {
        var priority = await _priorityRepo.FindAsync(workspaceId, priorityName.Trim());
        if (priority == null)
            throw new InvalidOperationException(string.Format(ErrorPriorityNotFound, priorityName));
        return priority;
    }

    private async Task LogPriorityChangeAsync(
        int workspaceId,
        int ticketId,
        int updatedByUserId,
        string priorityName,
        string? reason)
    {
        var note = BuildChangeNote($"Priority changed to '{priorityName}'", reason);

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = updatedByUserId,
            Action = ActionPriorityChanged,
            Note = note
        });
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
        var ticket = await GetTicketOrThrowAsync(workspaceId, ticketId);
        ValidateNotEmpty(newStatus, ErrorStatusEmpty);

        var status = await GetStatusOrThrowAsync(workspaceId, newStatus);
        
        ticket.StatusId = status.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _ticketRepo.UpdateAsync(ticket);
        await LogStatusChangeAsync(workspaceId, ticketId, updatedByUserId, status.Name, reason);

        return ticket;
    }

    private async Task<TicketStatus> GetStatusOrThrowAsync(int workspaceId, string statusName)
    {
        var status = await _statusRepo.FindByNameAsync(workspaceId, statusName.Trim());
        if (status == null)
            throw new InvalidOperationException(string.Format(ErrorStatusNotFound, statusName));
        return status;
    }

    private async Task LogStatusChangeAsync(
        int workspaceId,
        int ticketId,
        int updatedByUserId,
        string statusName,
        string? reason)
    {
        var note = BuildChangeNote($"Status changed to '{statusName}'", reason);

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = updatedByUserId,
            Action = ActionStatusChanged,
            Note = note
        });
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
        var ticket = await GetTicketOrThrowAsync(workspaceId, ticketId);
        ValidateNotEmpty(note, ErrorNoteEmpty);

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = addedByUserId,
            Action = ActionNoteAdded,
            Note = note.Trim()
        });

        return ticket;
    }

    private static void ValidateNotEmpty(string value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(errorMessage);
    }

    private static string BuildChangeNote(string baseNote, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return baseNote;

        return $"{baseNote}. Reason: {reason}";
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
