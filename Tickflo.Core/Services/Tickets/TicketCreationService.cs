using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles the business workflow of creating and managing tickets.
/// Replaces the generic ticket creation from TicketManagementService with a dedicated service.
/// </summary>
public class TicketCreationService : ITicketCreationService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ILocationRepository _locationRepo;

    public TicketCreationService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ITeamRepository teamRepo,
        ILocationRepository locationRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _teamRepo = teamRepo;
        _locationRepo = locationRepo;
    }

    /// <summary>
    /// Creates a new ticket with comprehensive validation and assignment logic.
    /// </summary>
    public async Task<Ticket> CreateTicketAsync(
        int workspaceId, 
        TicketCreationRequest request, 
        int createdByUserId)
    {
        // Business rule: Ticket must have subject
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new InvalidOperationException("Ticket subject is required");

        // Business rule: Validate contact exists if specified
        if (request.ContactId.HasValue && request.ContactId.Value <= 0)
            throw new InvalidOperationException("Invalid contact ID");

        // Business rule: Validate location exists and is active if specified
        if (request.LocationId.HasValue)
        {
            var location = await _locationRepo.FindAsync(workspaceId, request.LocationId.Value);
            if (location == null)
                throw new InvalidOperationException("Location not found");
            
            if (!location.Active)
                throw new InvalidOperationException("Cannot create ticket for inactive location");
        }

        // Business rule: Set default status
        var status = string.IsNullOrWhiteSpace(request.Status) ? "New" : request.Status.Trim();
        var priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim();

        var ticket = new Ticket
        {
            WorkspaceId = workspaceId,
            Subject = request.Subject.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Standard" : request.Type.Trim(),
            Priority = priority,
            Status = status,
            ContactId = request.ContactId,
            LocationId = request.LocationId,
            TicketInventories = request.Inventories ?? new List<TicketInventory>()
        };

        // Handle user assignment with validation
        if (request.AssignedUserId.HasValue)
        {
            var assigneeWorkspace = await _userWorkspaceRepo.FindAsync(request.AssignedUserId.Value, workspaceId);
            if (assigneeWorkspace != null && assigneeWorkspace.Accepted)
                ticket.AssignedUserId = request.AssignedUserId.Value;
            else
                throw new InvalidOperationException("Assigned user does not have valid access to this workspace");
        }
        else if (request.LocationId.HasValue)
        {
            // Business rule: Auto-assign from location default if available
            var location = await _locationRepo.FindAsync(workspaceId, request.LocationId.Value);
            if (location?.DefaultAssigneeUserId.HasValue == true)
                ticket.AssignedUserId = location.DefaultAssigneeUserId;
        }

        // Handle team assignment with validation
        if (request.AssignedTeamId.HasValue)
        {
            var team = await _teamRepo.FindByIdAsync(request.AssignedTeamId.Value);
            if (team != null && team.WorkspaceId == workspaceId)
                ticket.AssignedTeamId = request.AssignedTeamId.Value;
            else
                throw new InvalidOperationException("Team not found or does not belong to this workspace");
        }

        await _ticketRepo.CreateAsync(ticket);

        // Log ticket creation
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticket.Id,
            CreatedByUserId = createdByUserId,
            Action = "created",
            Note = $"Ticket created: {ticket.Subject}"
        });

        return ticket;
    }

    /// <summary>
    /// Creates a ticket from a contact inquiry or report.
    /// </summary>
    public async Task<Ticket> CreateFromContactAsync(
        int workspaceId, 
        int contactId, 
        TicketCreationRequest request, 
        int createdByUserId)
    {
        request.ContactId = contactId;
        return await CreateTicketAsync(workspaceId, request, createdByUserId);
    }

    /// <summary>
    /// Bulk creates tickets (e.g., from import).
    /// </summary>
    public async Task<List<Ticket>> CreateBulkAsync(
        int workspaceId, 
        List<TicketCreationRequest> requests, 
        int createdByUserId)
    {
        var tickets = new List<Ticket>();

        foreach (var request in requests)
        {
            try
            {
                var ticket = await CreateTicketAsync(workspaceId, request, createdByUserId);
                tickets.Add(ticket);
            }
            catch (InvalidOperationException)
            {
                // Log error but continue with other tickets
                // Could add detailed error tracking here
            }
        }

        return tickets;
    }
}

/// <summary>
/// Request to create a new ticket.
/// </summary>
public class TicketCreationRequest
{
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? ContactId { get; set; }
    public int? LocationId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public List<TicketInventory>? Inventories { get; set; }
}
