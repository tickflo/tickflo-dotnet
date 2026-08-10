namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

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

public class TicketCreationService(TickfloDbContext dbContext) : ITicketCreationService
{
    private const string ErrorSubjectRequired = "Ticket subject is required";
    private const string ErrorInvalidContactId = "Invalid contact ID";
    private const string ErrorLocationNotFound = "Location not found";
    private const string ErrorLocationInactive = "Cannot create ticket for inactive location";
    private const string ErrorInvalidAssignee = "Assigned user does not have valid access to this workspace";
    private const string ErrorInvalidTeam = "Team not found or does not belong to this workspace";

    private readonly TickfloDbContext dbContext = dbContext;

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

        // Ensure WorkspaceId is set on all ticket-inventory join records
        foreach (var ti in ticket.TicketInventories)
        {
            ti.WorkspaceId = workspaceId;
        }

        await this.AssignUserToTicketAsync(workspaceId, ticket, request);
        await this.AssignTeamToTicketAsync(workspaceId, ticket, request);

        this.dbContext.Tickets.Add(ticket);
        await this.dbContext.SaveChangesAsync();

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

        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId.Value)
            ?? throw new InvalidOperationException(ErrorLocationNotFound);

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

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var typeName = request.Type.Trim().ToLower();
            var type = await this.dbContext.TicketTypes
                .FirstOrDefaultAsync(ticketType => ticketType.WorkspaceId == workspaceId && ticketType.Name.ToLower() == typeName);
            if (type != null)
            {
                return type.Id;
            }
        }

        return await this.dbContext.TicketTypes
            .Where(ticketType => ticketType.WorkspaceId == workspaceId)
            .OrderBy(ticketType => ticketType.SortOrder)
            .ThenBy(ticketType => ticketType.Id)
            .Select(ticketType => (int?)ticketType.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<int?> ResolvePriorityIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.PriorityId.HasValue)
        {
            return request.PriorityId;
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priorityName = request.Priority.Trim().ToLower();
            var priority = await this.dbContext.TicketPriorities
                .FirstOrDefaultAsync(ticketPriority => ticketPriority.WorkspaceId == workspaceId && ticketPriority.Name.ToLower() == priorityName);
            if (priority != null)
            {
                return priority.Id;
            }
        }

        return await this.dbContext.TicketPriorities
            .Where(ticketPriority => ticketPriority.WorkspaceId == workspaceId)
            .OrderBy(ticketPriority => ticketPriority.SortOrder)
            .ThenBy(ticketPriority => ticketPriority.Id)
            .Select(ticketPriority => (int?)ticketPriority.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<int?> ResolveStatusIdAsync(int workspaceId, TicketCreationRequest request)
    {
        if (request.StatusId.HasValue)
        {
            return request.StatusId;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusName = request.Status.Trim().ToLower();
            var status = await this.dbContext.TicketStatuses
                .FirstOrDefaultAsync(ticketStatus => ticketStatus.WorkspaceId == workspaceId && ticketStatus.Name.ToLower() == statusName);
            if (status != null)
            {
                return status.Id;
            }
        }

        return await this.dbContext.TicketStatuses
            .Where(ticketStatus => ticketStatus.WorkspaceId == workspaceId && !ticketStatus.IsClosedState)
            .OrderBy(ticketStatus => ticketStatus.SortOrder)
            .ThenBy(ticketStatus => ticketStatus.Id)
            .Select(ticketStatus => (int?)ticketStatus.Id)
            .FirstOrDefaultAsync();
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
            TicketInventories = request.Inventories ?? [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
        var assigneeWorkspace = await this.dbContext.UserWorkspaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId);

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
        var location = await this.dbContext.Locations
            .FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId);

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

        var team = await this.dbContext.Teams.FindAsync(request.AssignedTeamId.Value);
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
        var history = new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = createdByUserId,
            Action = TicketHistoryAction.Created,
            Note = $"Ticket created: {subject}",
        };

        this.dbContext.TicketHistory.Add(history);
        await this.dbContext.SaveChangesAsync();
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
