using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Handles the business workflow of creating and managing tickets.
/// Replaces the generic ticket creation from TicketManagementService with a dedicated service.
/// </summary>
public class TicketCreationService : ITicketCreationService
{
    private const string DefaultTicketType = "Standard";
    private const string DefaultPriority = "Normal";
    private const string DefaultStatus = "New";
    private const string HistoryActionCreated = "created";
    
    private const string ErrorSubjectRequired = "Ticket subject is required";
    private const string ErrorInvalidContactId = "Invalid contact ID";
    private const string ErrorLocationNotFound = "Location not found";
    private const string ErrorLocationInactive = "Cannot create ticket for inactive location";
    private const string ErrorInvalidAssignee = "Assigned user does not have valid access to this workspace";
    private const string ErrorInvalidTeam = "Team not found or does not belong to this workspace";

    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;

    // Backward-compatible constructor for tests or simple usage
    public TicketCreationService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ITeamRepository teamRepo,
        ILocationRepository locationRepo)
        : this(ticketRepo, historyRepo, userWorkspaceRepo, teamRepo, locationRepo, 
               statusRepo: null!, priorityRepo: null!, typeRepo: null!)
    {
    }

    public TicketCreationService(
        ITicketRepository ticketRepo,
        ITicketHistoryRepository historyRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        ITeamRepository teamRepo,
        ILocationRepository locationRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _teamRepo = teamRepo;
        _locationRepo = locationRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
    }

    /// <summary>
    /// Creates a new ticket with comprehensive validation and assignment logic.
    /// </summary>
    public async Task<Ticket> CreateTicketAsync(
        int workspaceId,
        TicketCreationRequest request,
        int createdByUserId)
    {
        ValidateTicketRequest(request);
        await ValidateLocationAsync(workspaceId, request.LocationId);

        var typeId = await ResolveTicketTypeIdAsync(workspaceId, request);
        var priorityId = await ResolvePriorityIdAsync(workspaceId, request);
        var statusId = await ResolveStatusIdAsync(workspaceId, request);

        var ticket = BuildTicket(workspaceId, request, typeId, priorityId, statusId);

        await AssignUserToTicketAsync(workspaceId, ticket, request);
        await AssignTeamToTicketAsync(workspaceId, ticket, request);

        await _ticketRepo.CreateAsync(ticket);
        await CreateTicketHistoryAsync(workspaceId, ticket.Id, createdByUserId, ticket.Subject);

        return ticket;
    }

    private static void ValidateTicketRequest(TicketCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new InvalidOperationException(ErrorSubjectRequired);

        if (request.ContactId.HasValue && request.ContactId.Value <= 0)
            throw new InvalidOperationException(ErrorInvalidContactId);
    }

    private async Task ValidateLocationAsync(int workspaceId, int? locationId)
    {
        if (!locationId.HasValue)
            return;

        var location = await _locationRepo.FindAsync(workspaceId, locationId.Value);
        if (location == null)
            throw new InvalidOperationException(ErrorLocationNotFound);

        if (!location.Active)
            throw new InvalidOperationException(ErrorLocationInactive);
    }

    private async Task<int?> ResolveTicketTypeIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.TypeId.HasValue)
            return request.TypeId;

        var typeName = string.IsNullOrWhiteSpace(request.Type) ? DefaultTicketType : request.Type.Trim();
        var type = await _typeRepo.FindByNameAsync(workspaceId, typeName);
        return type?.Id;
    }

    private async Task<int?> ResolvePriorityIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.PriorityId.HasValue)
            return request.PriorityId;

        var priorityName = string.IsNullOrWhiteSpace(request.Priority) ? DefaultPriority : request.Priority.Trim();
        var priority = await _priorityRepo.FindAsync(workspaceId, priorityName);
        return priority?.Id;
    }

    private async Task<int?> ResolveStatusIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.StatusId.HasValue)
            return request.StatusId;

        var statusName = string.IsNullOrWhiteSpace(request.Status) ? DefaultStatus : request.Status.Trim();
        var status = await _statusRepo.FindByNameAsync(workspaceId, statusName);
        return status?.Id;
    }

    private static Ticket BuildTicket(
        int workspaceId,
        TicketCreationRequest request,
        int? typeId,
        int? priorityId,
        int? statusId)
    {
        return new Ticket
        {
            WorkspaceId = workspaceId,
            Subject = request.Subject.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
            TicketTypeId = typeId,
            PriorityId = priorityId,
            StatusId = statusId,
            ContactId = request.ContactId,
            LocationId = request.LocationId,
            TicketInventories = request.Inventories ?? new List<TicketInventory>()
        };
    }

    private async Task AssignUserToTicketAsync(int workspaceId, Ticket ticket, TicketCreationRequest request)
    {
        if (request.AssignedUserId.HasValue)
        {
            await ValidateAndAssignUserAsync(workspaceId, ticket, request.AssignedUserId.Value);
        }
        else if (request.LocationId.HasValue)
        {
            await AssignDefaultUserFromLocationAsync(workspaceId, ticket, request.LocationId.Value);
        }
    }

    private async Task ValidateAndAssignUserAsync(int workspaceId, Ticket ticket, int userId)
    {
        var assigneeWorkspace = await _userWorkspaceRepo.FindAsync(userId, workspaceId);
        if (assigneeWorkspace != null && assigneeWorkspace.Accepted)
        {
            ticket.AssignedUserId = userId;
        }
        else
        {
            throw new InvalidOperationException(ErrorInvalidAssignee);
        }
    }

    private async Task AssignDefaultUserFromLocationAsync(int workspaceId, Ticket ticket, int locationId)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location?.DefaultAssigneeUserId.HasValue == true)
        {
            ticket.AssignedUserId = location.DefaultAssigneeUserId;
        }
    }

    private async Task AssignTeamToTicketAsync(int workspaceId, Ticket ticket, TicketCreationRequest request)
    {
        if (!request.AssignedTeamId.HasValue)
            return;

        var team = await _teamRepo.FindByIdAsync(request.AssignedTeamId.Value);
        if (team != null && team.WorkspaceId == workspaceId)
        {
            ticket.AssignedTeamId = request.AssignedTeamId.Value;
        }
        else
        {
            throw new InvalidOperationException(ErrorInvalidTeam);
        }
    }

    private async Task CreateTicketHistoryAsync(int workspaceId, int ticketId, int createdByUserId, string subject)
    {
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = createdByUserId,
            Action = HistoryActionCreated,
            Note = $"Ticket created: {subject}"
        });
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
    public int? TypeId { get; set; }
    public int? PriorityId { get; set; }
    public int? StatusId { get; set; }
    public int? ContactId { get; set; }
    public int? LocationId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public List<TicketInventory>? Inventories { get; set; }
}
