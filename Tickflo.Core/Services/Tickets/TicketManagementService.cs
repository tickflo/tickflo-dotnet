namespace Tickflo.Core.Services.Tickets;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;

/// <summary>
/// Service for managing ticket lifecycle operations including creation, updates, and history tracking.
/// </summary>
public interface ITicketManagementService
{
    /// <summary>
    /// Creates a new ticket with the provided details.
    /// </summary>
    /// <param name="request">Ticket creation request</param>
    /// <returns>The created ticket</returns>
    public Task<Ticket> CreateTicketAsync(CreateTicketRequest request);

    /// <summary>
    /// Updates an existing ticket and logs changes to history.
    /// </summary>
    /// <param name="request">Ticket update request</param>
    /// <returns>The updated ticket</returns>
    public Task<Ticket> UpdateTicketAsync(UpdateTicketRequest request);

    /// <summary>
    /// Creates a ticket and dispatches its notification workflow.
    /// </summary>
    public Task<Ticket> CreateTicketAndNotifyAsync(CreateTicketRequest request);

    /// <summary>
    /// Updates a ticket and dispatches its notification workflow.
    /// </summary>
    public Task<Ticket> UpdateTicketAndNotifyAsync(UpdateTicketRequest request);

    /// <summary>
    /// Validates ticket assignment permissions.
    /// </summary>
    /// <param name="userId">User to assign</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>True if assignment is valid</returns>
    public Task<bool> ValidateUserAssignmentAsync(int userId, int workspaceId);

    /// <summary>
    /// Validates team assignment permissions.
    /// </summary>
    /// <param name="teamId">Team to assign</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>True if assignment is valid</returns>
    public Task<bool> ValidateTeamAssignmentAsync(int teamId, int workspaceId);

    /// <summary>
    /// Resolves the default assignee for a location.
    /// </summary>
    /// <param name="locationId">The location</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>User ID if a valid default assignee exists</returns>
    public Task<int?> ResolveDefaultAssigneeAsync(int locationId, int workspaceId);

    /// <summary>
    /// Checks if a user can access a ticket based on scope rules.
    /// </summary>
    /// <param name="ticket">The ticket to check</param>
    /// <param name="userId">User requesting access</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="isAdmin">Whether user is admin</param>
    /// <returns>True if user can access ticket</returns>
    public Task<bool> CanUserAccessTicketAsync(Ticket ticket, int userId, int workspaceId, bool isAdmin);

    /// <summary>
    /// Generates a display name for an assigned user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Formatted display name</returns>
    public Task<string?> GetAssigneeDisplayNameAsync(int userId);

    /// <summary>
    /// Generates an inventory summary for SignalR broadcast.
    /// </summary>
    /// <param name="inventories">Ticket inventories</param>
    /// <param name="workspaceId">Workspace context</param>
    /// <returns>Inventory summary and details</returns>
    public Task<(string summary, string details)> GenerateInventorySummaryAsync(
        List<TicketInventory> inventories,
        int workspaceId);

    /// <summary>
    /// Gets a ticket by workspace and ticket ID.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="ticketId">Ticket ID</param>
    /// <returns>The ticket, or null if not found</returns>
    public Task<Ticket?> GetTicketAsync(int workspaceId, int ticketId);
}

/// <summary>
/// Request to create a new ticket.
/// </summary>
public class CreateTicketRequest
{
    public int WorkspaceId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? TicketTypeId { get; set; }
    public int? PriorityId { get; set; }
    public int? StatusId { get; set; }
    public int? ContactId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public int? LocationId { get; set; }
    public List<TicketInventory> Inventories { get; set; } = [];
}

/// <summary>
/// Request to update an existing ticket.
/// </summary>
public class UpdateTicketRequest
{
    public int TicketId { get; set; }
    public int WorkspaceId { get; set; }
    public int UpdatedByUserId { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public int? TicketTypeId { get; set; }
    public int? PriorityId { get; set; }
    public int? StatusId { get; set; }
    public int? ContactId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedTeamId { get; set; }
    public int? LocationId { get; set; }
    public List<TicketInventory>? Inventories { get; set; }
}

public class TicketManagementService(
    TickfloDbContext dbContext,
    INotificationTriggerService notificationTriggerService) : ITicketManagementService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private readonly INotificationTriggerService notificationTriggerService = notificationTriggerService;

    private sealed record TicketNotificationSnapshot(
        string Subject,
        string Description,
        int? TicketTypeId,
        int? PriorityId,
        int? StatusId,
        int? ContactId,
        int? AssignedUserId,
        int? AssignedTeamId,
        int? LocationId)
    {
        public static TicketNotificationSnapshot FromTicket(Ticket ticket) => new(
            ticket.Subject,
            ticket.Description,
            ticket.TicketTypeId,
            ticket.PriorityId,
            ticket.StatusId,
            ticket.ContactId,
            ticket.AssignedUserId,
            ticket.AssignedTeamId,
            ticket.LocationId);

        public bool HasGeneralChanges(Ticket ticket) =>
            this.Subject != ticket.Subject ||
            this.Description != ticket.Description ||
            this.TicketTypeId != ticket.TicketTypeId ||
            this.PriorityId != ticket.PriorityId ||
            this.ContactId != ticket.ContactId ||
            this.LocationId != ticket.LocationId;
    }

    public async Task<Ticket> CreateTicketAsync(CreateTicketRequest request)
    {
        // Prefer direct IDs if provided, otherwise lookup by name
        var typeId = request.TicketTypeId ?? await this.ResolveTicketTypeIdAsync(request.WorkspaceId, request.Type);
        var priorityId = request.PriorityId ?? await this.ResolvePriorityIdAsync(request.WorkspaceId, request.Priority);
        var statusId = request.StatusId ?? await this.ResolveStatusIdAsync(request.WorkspaceId, request.Status);

        var ticket = new Ticket
        {
            WorkspaceId = request.WorkspaceId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            TicketTypeId = typeId,
            PriorityId = priorityId,
            StatusId = statusId,
            ContactId = request.ContactId,
            LocationId = request.LocationId,
            TicketInventories = request.Inventories,
            CreatedAt = DateTime.UtcNow
        };

        // Ensure WorkspaceId is set on ticket-inventory join records
        foreach (var ti in ticket.TicketInventories)
        {
            ti.WorkspaceId = request.WorkspaceId;
        }

        await this.AssignTicketUserAsync(ticket, request.AssignedUserId, request.LocationId, request.WorkspaceId);
        await this.AssignTicketTeamAsync(ticket, request.AssignedTeamId, request.WorkspaceId);

        this.dbContext.Tickets.Add(ticket);
        await this.dbContext.SaveChangesAsync();
        await this.CreateTicketHistoryAsync(request.WorkspaceId, ticket.Id, request.CreatedByUserId);

        return ticket;
    }

    public async Task<Ticket> CreateTicketAndNotifyAsync(CreateTicketRequest request)
    {
        var ticket = await this.CreateTicketAsync(request);
        await this.notificationTriggerService.NotifyTicketCreatedAsync(request.WorkspaceId, ticket, request.CreatedByUserId);
        return ticket;
    }

    private async Task<int?> ResolveTicketTypeIdAsync(int workspaceId, string? typeName)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            var typeNameLower = typeName.Trim().ToLower();
            var type = await this.dbContext.TicketTypes.FirstOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Name.ToLower() == typeNameLower);
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

    private async Task<int?> ResolvePriorityIdAsync(int workspaceId, string? priorityName)
    {
        if (!string.IsNullOrWhiteSpace(priorityName))
        {
            var priorityNameLower = priorityName.Trim().ToLower();
            var priority = await this.dbContext.TicketPriorities.FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.Name.ToLower() == priorityNameLower);
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

    private async Task<int?> ResolveStatusIdAsync(int workspaceId, string? statusName)
    {
        if (!string.IsNullOrWhiteSpace(statusName))
        {
            var statusNameLower = statusName.Trim().ToLower();
            var status = await this.dbContext.TicketStatuses.FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId && s.Name.ToLower() == statusNameLower);
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

    private async Task AssignTicketUserAsync(Ticket ticket, int? assignedUserId, int? locationId, int workspaceId)
    {
        if (assignedUserId.HasValue && await this.ValidateUserAssignmentAsync(assignedUserId.Value, workspaceId))
        {
            ticket.AssignedUserId = assignedUserId.Value;
            return;
        }

        if (locationId.HasValue)
        {
            var defaultAssignee = await this.ResolveDefaultAssigneeAsync(locationId.Value, workspaceId);
            if (defaultAssignee.HasValue)
            {
                ticket.AssignedUserId = defaultAssignee.Value;
            }
        }
    }

    private async Task AssignTicketTeamAsync(Ticket ticket, int? assignedTeamId, int workspaceId)
    {
        if (assignedTeamId.HasValue && await this.ValidateTeamAssignmentAsync(assignedTeamId.Value, workspaceId))
        {
            ticket.AssignedTeamId = assignedTeamId.Value;
        }
    }

    private async Task CreateTicketHistoryAsync(int workspaceId, int ticketId, int createdByUserId)
    {
        this.dbContext.TicketHistory.Add(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = createdByUserId,
            Action = TicketHistoryAction.Created,
            Note = "Ticket created",
            CreatedAt = DateTime.UtcNow
        });
        await this.dbContext.SaveChangesAsync();
    }

    public async Task<Ticket> UpdateTicketAsync(UpdateTicketRequest request)
    {
        var ticket = await this.dbContext.Tickets.FirstOrDefaultAsync(t => t.WorkspaceId == request.WorkspaceId && t.Id == request.TicketId) ?? throw new InvalidOperationException("Ticket not found");

        var changeTracker = new TicketChangeTracker(ticket);

        await this.UpdateTicketFieldsAsync(ticket, request);
        await this.UpdateTicketAssignmentsAsync(ticket, request);

        if (request.Inventories != null)
        {
            ticket.TicketInventories = request.Inventories;
            foreach (var ti in ticket.TicketInventories)
            {
                ti.WorkspaceId = request.WorkspaceId;
            }
        }

        await this.dbContext.SaveChangesAsync();
        await this.LogTicketChangesAsync(changeTracker, ticket, request);

        return ticket;
    }

    public async Task<Ticket> UpdateTicketAndNotifyAsync(UpdateTicketRequest request)
    {
        var existingTicket = await this.dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(ticket => ticket.WorkspaceId == request.WorkspaceId && ticket.Id == request.TicketId)
            ?? throw new InvalidOperationException("Ticket not found");

        var originalTicket = TicketNotificationSnapshot.FromTicket(existingTicket);
        var ticket = await this.UpdateTicketAsync(request);

        await this.NotifyTicketChangesAsync(request.WorkspaceId, ticket, request.UpdatedByUserId, originalTicket);
        return ticket;
    }

    private async Task UpdateTicketFieldsAsync(Ticket ticket, UpdateTicketRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            ticket.Subject = request.Subject.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            ticket.Description = request.Description.Trim();
        }

        // Prefer direct ID if provided, otherwise lookup by name
        if (request.TicketTypeId.HasValue)
        {
            ticket.TicketTypeId = request.TicketTypeId;
        }
        else if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var typeName = request.Type.Trim().ToLower();
            var type = await this.dbContext.TicketTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.WorkspaceId == request.WorkspaceId && t.Name.ToLower() == typeName);
            if (type != null)
            {
                ticket.TicketTypeId = type.Id;
            }
        }

        if (request.PriorityId.HasValue)
        {
            ticket.PriorityId = request.PriorityId;
        }
        else if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priorityName = request.Priority.Trim().ToLower();
            var priority = await this.dbContext.TicketPriorities
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId && p.Name.ToLower() == priorityName);
            if (priority != null)
            {
                ticket.PriorityId = priority.Id;
            }
        }

        if (request.StatusId.HasValue)
        {
            ticket.StatusId = request.StatusId;
        }
        else if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusName = request.Status.Trim().ToLower();
            var status = await this.dbContext.TicketStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.WorkspaceId == request.WorkspaceId && s.Name.ToLower() == statusName);
            if (status != null)
            {
                ticket.StatusId = status.Id;
            }
        }

        if (request.ContactId.HasValue)
        {
            ticket.ContactId = request.ContactId.Value;
        }

        if (request.LocationId.HasValue)
        {
            ticket.LocationId = request.LocationId.Value;
        }
    }

    private async Task UpdateTicketAssignmentsAsync(Ticket ticket, UpdateTicketRequest request)
    {
        if (request.AssignedUserId.HasValue)
        {
            var isValid = await this.ValidateUserAssignmentAsync(request.AssignedUserId.Value, request.WorkspaceId);

            if (isValid)
            {
                ticket.AssignedUserId = request.AssignedUserId.Value;
            }
        }

        if (request.AssignedTeamId.HasValue)
        {
            var isValid = await this.ValidateTeamAssignmentAsync(request.AssignedTeamId.Value, request.WorkspaceId);

            if (isValid)
            {
                ticket.AssignedTeamId = request.AssignedTeamId.Value;
            }
        }
    }

    private async Task LogTicketChangesAsync(TicketChangeTracker changeTracker, Ticket ticket, UpdateTicketRequest request)
    {
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Subject, changeTracker.OldSubject, ticket.Subject);
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Description, changeTracker.OldDescription, ticket.Description);
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Type, changeTracker.OldTypeId?.ToString(), ticket.TicketTypeId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Priority, changeTracker.OldPriorityId?.ToString(), ticket.PriorityId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Status, changeTracker.OldStatusId?.ToString(), ticket.StatusId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Contact, changeTracker.OldContactId?.ToString(), ticket.ContactId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.AssignedUser, changeTracker.OldAssignedUserId?.ToString(), ticket.AssignedUserId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            TicketHistoryField.Location, changeTracker.OldLocationId?.ToString(), ticket.LocationId?.ToString());

        if (request.Inventories != null)
        {
            await this.LogInventoryChangesAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
                changeTracker.OldInventories, ticket.TicketInventories?.ToList() ?? []);
        }
    }

    public async Task<bool> ValidateUserAssignmentAsync(int userId, int workspaceId) => await this.dbContext.UserWorkspaces.AnyAsync(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId && uw.Accepted);

    public async Task<bool> ValidateTeamAssignmentAsync(int teamId, int workspaceId) => await this.dbContext.Teams.AnyAsync(t => t.Id == teamId && t.WorkspaceId == workspaceId);

    public async Task<int?> ResolveDefaultAssigneeAsync(int locationId, int workspaceId)
    {
        var location = await this.dbContext.Locations.FirstOrDefaultAsync(l => l.WorkspaceId == workspaceId && l.Id == locationId);
        if (location?.DefaultAssigneeUserId == null)
        {
            return null;
        }

        // Verify user is in workspace
        if (await this.ValidateUserAssignmentAsync(location.DefaultAssigneeUserId.Value, workspaceId))
        {
            return location.DefaultAssigneeUserId.Value;
        }

        return null;
    }

    public async Task<bool> CanUserAccessTicketAsync(Ticket ticket, int userId, int workspaceId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        var scope = await this.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
        if (string.IsNullOrEmpty(scope))
        {
            return false;
        }

        return scope.ToLower() switch
        {
            "all" => true,
            "mine" => ticket.AssignedUserId == userId,
            "team" => await this.CanUserAccessTeamTicketAsync(ticket, userId),
            _ => false
        };
    }

    private async Task<string?> GetTicketViewScopeForUserAsync(int workspaceId, int userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return "all";
        }

        var userRoles = await this.dbContext.UserWorkspaceRoles
            .Where(uwr => uwr.WorkspaceId == workspaceId && uwr.UserId == userId)
            .Select(uwr => uwr.RoleId)
            .ToListAsync();

        if (userRoles.Count == 0)
        {
            return null;
        }

        var scope = await this.dbContext.RolePermissions
            .Where(rp => userRoles.Contains(rp.RoleId))
            .Join(this.dbContext.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
            .Where(p => p.Resource == "tickets_scope")
            .Select(p => p.Action)
            .FirstOrDefaultAsync();

        return scope;
    }

    private async Task<bool> CanUserAccessTeamTicketAsync(Ticket ticket, int userId)
    {
        if (ticket.AssignedUserId == userId)
        {
            return true;
        }

        if (!ticket.AssignedTeamId.HasValue)
        {
            return false;
        }

        return await this.dbContext.TeamMembers.AnyAsync(tm => tm.TeamId == ticket.AssignedTeamId.Value && tm.UserId == userId);
    }

    public async Task<string?> GetAssigneeDisplayNameAsync(int userId)
    {
        var user = await this.dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        var name = user.Name?.Trim() ?? "(unknown)";
        var email = user.Email?.Trim();

        return string.IsNullOrEmpty(email) ? name : $"{name} ({email})";
    }

    public async Task<(string summary, string details)> GenerateInventorySummaryAsync(
        List<TicketInventory> inventories,
        int workspaceId)
    {
        if (inventories == null || inventories.Count == 0)
        {
            return ("—", string.Empty);
        }

        var count = inventories.Count;
        var total = inventories.Sum(iv => iv.UnitPrice * iv.Quantity);
        var summary = FormatInventorySummary(count, total);
        var details = await this.GenerateInventoryDetailsAsync(inventories, workspaceId);

        return (summary, details);
    }

    private static string FormatInventorySummary(int count, decimal total)
    {
        var itemText = count == 1 ? "item" : "items";
        return $"{count} {itemText} · ${total:F2}";
    }

    private async Task<string> GenerateInventoryDetailsAsync(List<TicketInventory> inventories, int workspaceId)
    {
        var detailParts = new List<string>();

        foreach (var iv in inventories)
        {
            var name = await this.GetInventoryNameAsync(iv, workspaceId);
            detailParts.Add($"{name} x{iv.Quantity}");
        }

        return string.Join(", ", detailParts);
    }

    private async Task<string> GetInventoryNameAsync(TicketInventory ticketInventory, int workspaceId)
    {
        var name = ticketInventory.Inventory?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var inventory = await this.dbContext.Inventory.FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Id == ticketInventory.InventoryId);
        return inventory?.Name ?? $"Item #{ticketInventory.InventoryId}";
    }

    private async Task LogFieldChangeAsync(
        int workspaceId,
        int ticketId,
        int userId,
        TicketHistoryField field,
        string? oldValue,
        string? newValue)
    {
        var oldTrim = oldValue?.Trim() ?? string.Empty;
        var newTrim = newValue?.Trim() ?? string.Empty;

        if (oldTrim == newTrim)
        {
            return;
        }

        this.dbContext.TicketHistory.Add(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = userId,
            Action = TicketHistoryAction.FieldChanged,
            Field = field,
            OldValue = string.IsNullOrEmpty(oldTrim) ? null : oldTrim,
            NewValue = string.IsNullOrEmpty(newTrim) ? null : newTrim,
            CreatedAt = DateTime.UtcNow
        });
        await this.dbContext.SaveChangesAsync();
    }

    private async Task LogInventoryChangesAsync(
        int workspaceId,
        int ticketId,
        int userId,
        List<TicketInventory> oldInventories,
        List<TicketInventory> newInventories)
    {
        var oldSummary = await this.GenerateInventorySummaryForHistoryAsync(oldInventories, workspaceId);
        var newSummary = await this.GenerateInventorySummaryForHistoryAsync(newInventories, workspaceId);
        await this.LogFieldChangeAsync(workspaceId, ticketId, userId, TicketHistoryField.Inventory, oldSummary, newSummary);
    }

    private async Task<string> GenerateInventorySummaryForHistoryAsync(
        List<TicketInventory> inventories,
        int workspaceId)
    {
        if (inventories == null || inventories.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var iv in inventories)
        {
            var name = await this.GetInventoryNameAsync(iv, workspaceId);
            parts.Add($"{name} x{iv.Quantity} @ ${iv.UnitPrice:F2}");
        }

        return string.Join(", ", parts);
    }

    private async Task NotifyTicketChangesAsync(
        int workspaceId,
        Ticket ticket,
        int userId,
        TicketNotificationSnapshot originalTicket)
    {
        var assignmentChanged = originalTicket.AssignedUserId != ticket.AssignedUserId ||
            originalTicket.AssignedTeamId != ticket.AssignedTeamId;
        var statusChanged = originalTicket.StatusId != ticket.StatusId;
        var generalChangesDetected = originalTicket.HasGeneralChanges(ticket);

        if (assignmentChanged)
        {
            await this.notificationTriggerService.NotifyTicketAssignmentChangedAsync(
                workspaceId,
                ticket,
                originalTicket.AssignedUserId,
                originalTicket.AssignedTeamId,
                userId);
        }

        string? oldStatusName = null;
        string? newStatusName = null;
        if (statusChanged)
        {
            oldStatusName = await this.GetStatusDisplayNameAsync(workspaceId, originalTicket.StatusId);
            newStatusName = await this.GetStatusDisplayNameAsync(workspaceId, ticket.StatusId);

            await this.notificationTriggerService.NotifyTicketStatusChangedAsync(
                workspaceId,
                ticket,
                oldStatusName ?? "Unknown",
                newStatusName ?? "Unknown",
                userId);
        }

        var changeSummary = BuildChangeSummary(assignmentChanged, statusChanged, generalChangesDetected, oldStatusName, newStatusName);
        if (changeSummary != null)
        {
            await this.notificationTriggerService.NotifyTicketUpdatedAsync(
                workspaceId,
                ticket,
                userId,
                changeSummary,
                assignmentChanged && ticket.AssignedUserId.HasValue ? [ticket.AssignedUserId.Value] : null);
        }
    }

    private async Task<string?> GetStatusDisplayNameAsync(int workspaceId, int? statusId)
    {
        if (!statusId.HasValue)
        {
            return null;
        }

        return await this.dbContext.TicketStatuses
            .Where(status => status.WorkspaceId == workspaceId && status.Id == statusId.Value)
            .Select(status => status.Name)
            .FirstOrDefaultAsync();
    }

    private static string? BuildChangeSummary(
        bool assignmentChanged,
        bool statusChanged,
        bool generalChangesDetected,
        string? oldStatusName,
        string? newStatusName)
    {
        var changes = new List<string>();

        if (assignmentChanged)
        {
            changes.Add("Assignment changed.");
        }

        if (statusChanged)
        {
            changes.Add($"Status changed from '{oldStatusName ?? "Unknown"}' to '{newStatusName ?? "Unknown"}'.");
        }

        if (generalChangesDetected)
        {
            changes.Add("Ticket details were updated.");
        }

        return changes.Count == 0 ? null : string.Join(" ", changes);
    }

    private sealed class TicketChangeTracker(Ticket ticket)
    {
        public string OldSubject { get; } = ticket.Subject;
        public string OldDescription { get; } = ticket.Description;
        public int? OldTypeId { get; } = ticket.TicketTypeId;
        public int? OldPriorityId { get; } = ticket.PriorityId;
        public int? OldStatusId { get; } = ticket.StatusId;
        public int? OldContactId { get; } = ticket.ContactId;
        public int? OldAssignedUserId { get; } = ticket.AssignedUserId;
        public int? OldLocationId { get; } = ticket.LocationId;
        public List<TicketInventory> OldInventories { get; } = ticket.TicketInventories?.ToList() ?? [];
    }

    public async Task<Ticket?> GetTicketAsync(int workspaceId, int ticketId) =>
        await this.dbContext.Tickets
            .FirstOrDefaultAsync(ticket => ticket.WorkspaceId == workspaceId && ticket.Id == ticketId);
}


