namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of creating and managing tickets.
/// Replaces the generic ticket creation from TicketManagementService with a dedicated service.
/// </summary>

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
    public Task<Ticket> CreateTicketAsync(int workspaceId, TicketCreationRequest request, int createdByUserId);

    /// <summary>
    /// Creates a ticket linked to a specific contact.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="contactId">Contact the ticket relates to</param>
    /// <param name="request">Ticket creation details</param>
    /// <param name="createdByUserId">User creating the ticket</param>
    /// <returns>The created ticket</returns>
    public Task<Ticket> CreateFromContactAsync(int workspaceId, int contactId, TicketCreationRequest request, int createdByUserId);

    /// <summary>
    /// Bulk creates multiple tickets (e.g., from import).
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="requests">Ticket creation requests</param>
    /// <param name="createdByUserId">User creating tickets</param>
    /// <returns>List of created tickets</returns>
    public Task<List<Ticket>> CreateBulkAsync(int workspaceId, List<TicketCreationRequest> requests, int createdByUserId);
}

public class TicketCreationService(
    ITicketRepository ticketRepository,
    ITicketHistoryRepository historyRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    ITeamRepository teamRepository,
    ILocationRepository locationRepository,
    ITicketStatusRepository statusRepository,
    ITicketPriorityRepository priorityRepository,
    ITicketTypeRepository ticketTypeRepository) : ITicketCreationService
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

    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketHistoryRepository historyRepository = historyRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly ILocationRepository locationRepository = locationRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;
    private readonly ITicketTypeRepository ticketTypeRepository = ticketTypeRepository;

    // Backward-compatible constructor for tests or simple usage
    public TicketCreationService(
        ITicketRepository ticketRepository,
        ITicketHistoryRepository historyRepository,
        IUserWorkspaceRepository userWorkspaceRepository,
        ITeamRepository teamRepository,
        ILocationRepository locationRepository)
        : this(ticketRepository, historyRepository, userWorkspaceRepository, teamRepository, locationRepository,
               statusRepository: null!, priorityRepository: null!, ticketTypeRepository: null!)
    {
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
        await this.ValidateLocationAsync(workspaceId, request.LocationId);

        var typeId = await this.ResolveTicketTypeIdAsync(workspaceId, request);
        var priorityId = await this.ResolvePriorityIdAsync(workspaceId, request);
        var statusId = await this.ResolveStatusIdAsync(workspaceId, request);

        var ticket = BuildTicket(workspaceId, request, typeId, priorityId, statusId);

        await this.AssignUserToTicketAsync(workspaceId, ticket, request);
        await this.AssignTeamToTicketAsync(workspaceId, ticket, request);

        await this.ticketRepository.CreateAsync(ticket);
        await this.CreateTicketHistoryAsync(workspaceId, ticket.Id, createdByUserId, ticket.Subject);

        return ticket;
    }

    private static void ValidateTicketRequest(TicketCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new InvalidOperationException(ErrorSubjectRequired);
        }

        if (request.ContactId.HasValue && request.ContactId.Value <= 0)
        {
            throw new InvalidOperationException(ErrorInvalidContactId);
        }
    }

    private async Task ValidateLocationAsync(int workspaceId, int? locationId)
    {
        if (!locationId.HasValue)
        {
            return;
        }

        var location = await this.locationRepository.FindAsync(workspaceId, locationId.Value) ?? throw new InvalidOperationException(ErrorLocationNotFound);

        if (!location.Active)
        {
            throw new InvalidOperationException(ErrorLocationInactive);
        }
    }

    private async Task<int?> ResolveTicketTypeIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.TypeId.HasValue)
        {
            return request.TypeId;
        }

        var typeName = string.IsNullOrWhiteSpace(request.Type) ? DefaultTicketType : request.Type.Trim();
        var type = await this.ticketTypeRepository.FindByNameAsync(workspaceId, typeName);
        return type?.Id;
    }

    private async Task<int?> ResolvePriorityIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.PriorityId.HasValue)
        {
            return request.PriorityId;
        }

        var priorityName = string.IsNullOrWhiteSpace(request.Priority) ? DefaultPriority : request.Priority.Trim();
        var priority = await this.priorityRepository.FindAsync(workspaceId, priorityName);
        return priority?.Id;
    }

    private async Task<int?> ResolveStatusIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.StatusId.HasValue)
        {
            return request.StatusId;
        }

        var statusName = string.IsNullOrWhiteSpace(request.Status) ? DefaultStatus : request.Status.Trim();
        var status = await this.statusRepository.FindByNameAsync(workspaceId, statusName);
        return status?.Id;
    }

    private static Ticket BuildTicket(
        int workspaceId,
        TicketCreationRequest request,
        int? typeId,
        int? priorityId,
        int? statusId) => new()
        {
            WorkspaceId = workspaceId,
            Subject = request.Subject.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
            TicketTypeId = typeId,
            PriorityId = priorityId,
            StatusId = statusId,
            ContactId = request.ContactId,
            LocationId = request.LocationId,
            TicketInventories = request.Inventories ?? []
        };

    private async Task AssignUserToTicketAsync(int workspaceId, Ticket ticket, TicketCreationRequest request)
    {
        if (request.AssignedUserId.HasValue)
        {
            await this.ValidateAndAssignUserAsync(workspaceId, ticket, request.AssignedUserId.Value);
        }
        else if (request.LocationId.HasValue)
        {
            await this.AssignDefaultUserFromLocationAsync(workspaceId, ticket, request.LocationId.Value);
        }
    }

    private async Task ValidateAndAssignUserAsync(int workspaceId, Ticket ticket, int userId)
    {
        var assigneeWorkspace = await this.userWorkspaceRepository.FindAsync(userId, workspaceId);
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
        var location = await this.locationRepository.FindAsync(workspaceId, locationId);
        if (location?.DefaultAssigneeUserId.HasValue == true)
        {
            ticket.AssignedUserId = location.DefaultAssigneeUserId;
        }
    }

    private async Task AssignTeamToTicketAsync(int workspaceId, Ticket ticket, TicketCreationRequest request)
    {
        if (!request.AssignedTeamId.HasValue)
        {
            return;
        }

        var team = await this.teamRepository.FindByIdAsync(request.AssignedTeamId.Value);
        if (team != null && team.WorkspaceId == workspaceId)
        {
            ticket.AssignedTeamId = request.AssignedTeamId.Value;
        }
        else
        {
            throw new InvalidOperationException(ErrorInvalidTeam);
        }
    }

    private async Task CreateTicketHistoryAsync(int workspaceId, int ticketId, int createdByUserId, string subject) => await this.historyRepository.CreateAsync(new TicketHistory
    {
        WorkspaceId = workspaceId,
        TicketId = ticketId,
        CreatedByUserId = createdByUserId,
        Action = HistoryActionCreated,
        Note = $"Ticket created: {subject}"
    });

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
        return await this.CreateTicketAsync(workspaceId, request, createdByUserId);
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
                var ticket = await this.CreateTicketAsync(workspaceId, request, createdByUserId);
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
