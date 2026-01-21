namespace Tickflo.Core.Services.Tickets;

using System.Text;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of updating ticket information and tracking changes.
/// </summary>
public class TicketUpdateService(
    ITicketRepository ticketRepository,
    ITicketHistoryRepository historyRepository,
    ITicketStatusRepository statusRepository,
    ITicketPriorityRepository priorityRepository) : ITicketUpdateService
{
    private const string ActionUpdated = "updated";
    private const string ActionPriorityChanged = "priority_changed";
    private const string ActionStatusChanged = "status_changed";
    private const string ActionNoteAdded = "note_added";

    private const string ErrorTicketNotFound = "Ticket not found";
    private const string ErrorPriorityEmpty = "Priority cannot be empty";
    private static readonly CompositeFormat ErrorPriorityNotFound = CompositeFormat.Parse("Priority '{0}' not found in workspace");
    private const string ErrorStatusEmpty = "Status cannot be empty";
    private static readonly CompositeFormat ErrorStatusNotFound = CompositeFormat.Parse("Status '{0}' not found in workspace");
    private const string ErrorNoteEmpty = "Note cannot be empty";

    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketHistoryRepository historyRepository = historyRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;

    // Backward-compatible constructor
    public TicketUpdateService(
        ITicketRepository ticketRepository,
        ITicketHistoryRepository historyRepository)
        : this(ticketRepository, historyRepository, statusRepository: null!, priorityRepository: null!)
    { }

    /// <summary>
    /// Updates ticket core information (subject, description, etc.).
    /// </summary>
    public async Task<Ticket> UpdateTicketInfoAsync(
        int workspaceId,
        int ticketId,
        TicketUpdateRequest request,
        int updatedByUserId)
    {
        var ticket = await this.GetTicketOrThrowAsync(workspaceId, ticketId);
        var changes = TrackTicketChanges(ticket, request);

        if (changes.Count != 0)
        {
            ApplyTicketChanges(ticket, request);
            ticket.UpdatedAt = DateTime.UtcNow;
            await this.ticketRepository.UpdateAsync(ticket);
            await this.LogChangesAsync(workspaceId, ticketId, updatedByUserId, changes);
        }

        return ticket;
    }

    private async Task<Ticket> GetTicketOrThrowAsync(int workspaceId, int ticketId)
    {
        var ticket = await this.ticketRepository.FindAsync(workspaceId, ticketId) ?? throw new InvalidOperationException(ErrorTicketNotFound);

        return ticket;
    }

    private static List<string> TrackTicketChanges(Ticket ticket, TicketUpdateRequest request)
    {
        var changes = new List<string>();

        if (ShouldUpdateSubject(ticket, request))
        {
            changes.Add($"Subject changed from '{ticket.Subject}' to '{request.Subject!.Trim()}'");
        }

        if (ShouldUpdateDescription(ticket, request))
        {
            changes.Add("Description updated");
        }

        if (ShouldUpdateContact(ticket, request))
        {
            changes.Add($"Contact changed from {ticket.ContactId} to {request.ContactId}");
        }

        if (ShouldUpdateLocation(ticket, request))
        {
            changes.Add($"Location changed from {ticket.LocationId} to {request.LocationId}");
        }

        return changes;
    }

    private static bool ShouldUpdateSubject(Ticket ticket, TicketUpdateRequest request) => !string.IsNullOrWhiteSpace(request.Subject) && ticket.Subject != request.Subject.Trim();

    private static bool ShouldUpdateDescription(Ticket ticket, TicketUpdateRequest request) => !string.IsNullOrWhiteSpace(request.Description) && ticket.Description != request.Description.Trim();

    private static bool ShouldUpdateContact(Ticket ticket, TicketUpdateRequest request) => request.ContactId.HasValue && ticket.ContactId != request.ContactId.Value;

    private static bool ShouldUpdateLocation(Ticket ticket, TicketUpdateRequest request) => request.LocationId.HasValue && ticket.LocationId != request.LocationId.Value;

    private static void ApplyTicketChanges(Ticket ticket, TicketUpdateRequest request)
    {
        if (ShouldUpdateSubject(ticket, request))
        {
            ticket.Subject = request.Subject!.Trim();
        }

        if (ShouldUpdateDescription(ticket, request))
        {
            ticket.Description = request.Description!.Trim();
        }

        if (ShouldUpdateContact(ticket, request))
        {
            ticket.ContactId = request.ContactId!.Value;
        }

        if (ShouldUpdateLocation(ticket, request))
        {
            ticket.LocationId = request.LocationId!.Value;
        }
    }

    private async Task LogChangesAsync(int workspaceId, int ticketId, int updatedByUserId, List<string> changes) => await this.historyRepository.CreateAsync(new TicketHistory
    {
        WorkspaceId = workspaceId,
        TicketId = ticketId,
        CreatedByUserId = updatedByUserId,
        Action = ActionUpdated,
        Note = string.Join("; ", changes)
    });

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
        var ticket = await this.GetTicketOrThrowAsync(workspaceId, ticketId);
        ValidateNotEmpty(newPriority, ErrorPriorityEmpty);

        var priority = await this.GetPriorityOrThrowAsync(workspaceId, newPriority);

        ticket.PriorityId = priority.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);
        await this.LogPriorityChangeAsync(workspaceId, ticketId, updatedByUserId, priority.Name, reason);

        return ticket;
    }

    private async Task<TicketPriority> GetPriorityOrThrowAsync(int workspaceId, string priorityName)
    {
        var priority = await this.priorityRepository.FindAsync(workspaceId, priorityName.Trim()) ?? throw new InvalidOperationException(string.Format(null, ErrorPriorityNotFound, priorityName));

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

        await this.historyRepository.CreateAsync(new TicketHistory
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
        var ticket = await this.GetTicketOrThrowAsync(workspaceId, ticketId);
        ValidateNotEmpty(newStatus, ErrorStatusEmpty);

        var status = await this.GetStatusOrThrowAsync(workspaceId, newStatus);

        ticket.StatusId = status.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        await this.ticketRepository.UpdateAsync(ticket);
        await this.LogStatusChangeAsync(workspaceId, ticketId, updatedByUserId, status.Name, reason);

        return ticket;
    }

    private async Task<TicketStatus> GetStatusOrThrowAsync(int workspaceId, string statusName)
    {
        var status = await this.statusRepository.FindByNameAsync(workspaceId, statusName.Trim()) ?? throw new InvalidOperationException(string.Format(null, ErrorStatusNotFound, statusName));

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

        await this.historyRepository.CreateAsync(new TicketHistory
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
        var ticket = await this.GetTicketOrThrowAsync(workspaceId, ticketId);
        ValidateNotEmpty(note, ErrorNoteEmpty);

        await this.historyRepository.CreateAsync(new TicketHistory
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
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static string BuildChangeNote(string baseNote, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return baseNote;
        }

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
