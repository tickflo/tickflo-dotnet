namespace Tickflo.Core.Services.Tickets;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Service for managing ticket lifecycle operations including creation, updates, and history tracking.
/// </summary>
public class TicketManagementService(
    ITicketRepository ticketRepo,
    ITicketHistoryRepository historyRepo,
    IUserRepository userRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    ITeamRepository teamRepo,
    ITeamMemberRepository teamMemberRepo,
    ILocationRepository locationRepo,
    IInventoryRepository inventoryRepo,
    IRolePermissionRepository rolePermRepo,
    ITicketTypeRepository typeRepo,
    ITicketPriorityRepository priorityRepo,
    ITicketStatusRepository statusRepo) : ITicketManagementService
{
    private const string DefaultTicketType = "Standard";
    private const string DefaultPriority = "Normal";
    private const string DefaultStatus = "New";
    private const string HistoryActionCreated = "created";
    private const string HistoryActionFieldChanged = "field_changed";

    private readonly ITicketRepository _ticketRepo = ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo = historyRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly ITeamRepository _teamRepo = teamRepo;
    private readonly ITeamMemberRepository _teamMemberRepo = teamMemberRepo;
    private readonly ILocationRepository _locationRepo = locationRepo;
    private readonly IInventoryRepository _inventoryRepo = inventoryRepo;
    private readonly IRolePermissionRepository _rolePermRepo = rolePermRepo;
    private readonly ITicketTypeRepository _typeRepo = typeRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;
    private readonly ITicketStatusRepository _statusRepo = statusRepo;

    public async Task<Ticket> CreateTicketAsync(CreateTicketRequest request)
    {
        var typeId = await this.ResolveTicketTypeIdAsync(request.WorkspaceId, request.Type);
        var priorityId = await this.ResolvePriorityIdAsync(request.WorkspaceId, request.Priority);
        var statusId = await this.ResolveStatusIdAsync(request.WorkspaceId, request.Status);

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
            TicketInventories = request.Inventories
        };

        await this.AssignTicketUserAsync(ticket, request.AssignedUserId, request.LocationId, request.WorkspaceId);
        await this.AssignTicketTeamAsync(ticket, request.AssignedTeamId, request.WorkspaceId);

        await this._ticketRepo.CreateAsync(ticket);
        await this.CreateTicketHistoryAsync(request.WorkspaceId, ticket.Id, request.CreatedByUserId);

        return ticket;
    }

    private async Task<int?> ResolveTicketTypeIdAsync(int workspaceId, string? typeName)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            var type = await this._typeRepo.FindByNameAsync(workspaceId, typeName.Trim());
            if (type != null)
            {
                return type.Id;
            }
        }

        var defaultType = await this._typeRepo.FindByNameAsync(workspaceId, DefaultTicketType);
        return defaultType?.Id;
    }

    private async Task<int?> ResolvePriorityIdAsync(int workspaceId, string? priorityName)
    {
        if (!string.IsNullOrWhiteSpace(priorityName))
        {
            var priority = await this._priorityRepo.FindAsync(workspaceId, priorityName.Trim());
            if (priority != null)
            {
                return priority.Id;
            }
        }

        var defaultPriority = await this._priorityRepo.FindAsync(workspaceId, DefaultPriority);
        return defaultPriority?.Id;
    }

    private async Task<int?> ResolveStatusIdAsync(int workspaceId, string? statusName)
    {
        if (!string.IsNullOrWhiteSpace(statusName))
        {
            var status = await this._statusRepo.FindByNameAsync(workspaceId, statusName.Trim());
            if (status != null)
            {
                return status.Id;
            }
        }

        var defaultStatus = await this._statusRepo.FindByNameAsync(workspaceId, DefaultStatus);
        return defaultStatus?.Id;
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

    private async Task CreateTicketHistoryAsync(int workspaceId, int ticketId, int createdByUserId) => await this._historyRepo.CreateAsync(new TicketHistory
    {
        WorkspaceId = workspaceId,
        TicketId = ticketId,
        CreatedByUserId = createdByUserId,
        Action = HistoryActionCreated,
        Note = "Ticket created"
    });

    public async Task<Ticket> UpdateTicketAsync(UpdateTicketRequest request)
    {
        var ticket = await this._ticketRepo.FindAsync(request.WorkspaceId, request.TicketId) ?? throw new InvalidOperationException("Ticket not found");

        var changeTracker = new TicketChangeTracker(ticket);

        await this.UpdateTicketFieldsAsync(ticket, request);
        await this.UpdateTicketAssignmentsAsync(ticket, request);

        if (request.Inventories != null)
        {
            ticket.TicketInventories = request.Inventories;
        }

        await this._ticketRepo.UpdateAsync(ticket);
        await this.LogTicketChangesAsync(changeTracker, ticket, request);

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

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = await this._typeRepo.FindByNameAsync(request.WorkspaceId, request.Type.Trim());
            if (type != null)
            {
                ticket.TicketTypeId = type.Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = await this._priorityRepo.FindAsync(request.WorkspaceId, request.Priority.Trim());
            if (priority != null)
            {
                ticket.PriorityId = priority.Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = await this._statusRepo.FindByNameAsync(request.WorkspaceId, request.Status.Trim());
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
        if (request.AssignedUserId.HasValue && await this.ValidateUserAssignmentAsync(request.AssignedUserId.Value, request.WorkspaceId))
        {
            ticket.AssignedUserId = request.AssignedUserId.Value;
        }

        if (request.AssignedTeamId.HasValue && await this.ValidateTeamAssignmentAsync(request.AssignedTeamId.Value, request.WorkspaceId))
        {
            ticket.AssignedTeamId = request.AssignedTeamId.Value;
        }
    }

    private async Task LogTicketChangesAsync(TicketChangeTracker changeTracker, Ticket ticket, UpdateTicketRequest request)
    {
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "Subject", changeTracker.OldSubject, ticket.Subject);
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "Description", changeTracker.OldDescription, ticket.Description);
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "TicketTypeId", changeTracker.OldTypeId?.ToString(), ticket.TicketTypeId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "PriorityId", changeTracker.OldPriorityId?.ToString(), ticket.PriorityId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "StatusId", changeTracker.OldStatusId?.ToString(), ticket.StatusId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "ContactId", changeTracker.OldContactId?.ToString(), ticket.ContactId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "AssignedUserId", changeTracker.OldAssignedUserId?.ToString(), ticket.AssignedUserId?.ToString());
        await this.LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
            "LocationId", changeTracker.OldLocationId?.ToString(), ticket.LocationId?.ToString());

        if (request.Inventories != null)
        {
            await this.LogInventoryChangesAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId,
                changeTracker.OldInventories, ticket.TicketInventories?.ToList() ?? []);
        }
    }

    public async Task<bool> ValidateUserAssignmentAsync(int userId, int workspaceId)
    {
        var memberships = await this._userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        return memberships.Any(m => m.UserId == userId && m.Accepted);
    }

    public async Task<bool> ValidateTeamAssignmentAsync(int teamId, int workspaceId)
    {
        var team = await this._teamRepo.FindByIdAsync(teamId);
        return team != null && team.WorkspaceId == workspaceId;
    }

    public async Task<int?> ResolveDefaultAssigneeAsync(int locationId, int workspaceId)
    {
        var location = await this._locationRepo.FindAsync(workspaceId, locationId);
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

        var scope = await this._rolePermRepo.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
        if (string.IsNullOrEmpty(scope))
        {
            return false;
        }

        return scope.ToLowerInvariant() switch
        {
            "all" => true,
            "mine" => ticket.AssignedUserId == userId,
            "team" => await this.CanUserAccessTeamTicketAsync(ticket, userId, workspaceId),
            _ => false
        };
    }

    private async Task<bool> CanUserAccessTeamTicketAsync(Ticket ticket, int userId, int workspaceId)
    {
        if (ticket.AssignedUserId == userId)
        {
            return true;
        }

        if (!ticket.AssignedTeamId.HasValue)
        {
            return false;
        }

        var myTeams = await this._teamMemberRepo.ListTeamsForUserAsync(workspaceId, userId);
        return myTeams.Any(t => t.Id == ticket.AssignedTeamId.Value);
    }

    public async Task<string?> GetAssigneeDisplayNameAsync(int userId)
    {
        var user = await this._userRepo.FindByIdAsync(userId);
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

        var inventory = await this._inventoryRepo.FindAsync(workspaceId, ticketInventory.InventoryId);
        return inventory?.Name ?? $"Item #{ticketInventory.InventoryId}";
    }

    private async Task LogFieldChangeAsync(
        int workspaceId,
        int ticketId,
        int userId,
        string field,
        string? oldValue,
        string? newValue)
    {
        var oldTrim = oldValue?.Trim() ?? string.Empty;
        var newTrim = newValue?.Trim() ?? string.Empty;

        if (oldTrim == newTrim)
        {
            return;
        }

        await this._historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = userId,
            Action = HistoryActionFieldChanged,
            Field = field,
            OldValue = string.IsNullOrEmpty(oldTrim) ? null : oldTrim,
            NewValue = string.IsNullOrEmpty(newTrim) ? null : newTrim
        });
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
        await this.LogFieldChangeAsync(workspaceId, ticketId, userId, "Inventory", oldSummary, newSummary);
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
}


