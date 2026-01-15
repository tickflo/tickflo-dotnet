using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Tickets;

/// <summary>
/// Service for managing ticket lifecycle operations including creation, updates, and history tracking.
/// </summary>
public class TicketManagementService : ITicketManagementService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ITeamMemberRepository _teamMemberRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IRolePermissionRepository _rolePermRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketStatusRepository _statusRepo;

    public TicketManagementService(
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
        ITicketStatusRepository statusRepo)
    {
        _ticketRepo = ticketRepo;
        _historyRepo = historyRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _teamRepo = teamRepo;
        _teamMemberRepo = teamMemberRepo;
        _locationRepo = locationRepo;
        _inventoryRepo = inventoryRepo;
        _rolePermRepo = rolePermRepo;
        _typeRepo = typeRepo;
        _priorityRepo = priorityRepo;
        _statusRepo = statusRepo;
    }

    public async Task<Ticket> CreateTicketAsync(CreateTicketRequest request)
    {
        // Resolve IDs from names
        int? typeId = null;
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = await _typeRepo.FindByNameAsync(request.WorkspaceId, request.Type.Trim());
            typeId = type?.Id;
        }
        if (!typeId.HasValue)
        {
            var defaultType = await _typeRepo.FindByNameAsync(request.WorkspaceId, "Standard");
            typeId = defaultType?.Id;
        }

        int? priorityId = null;
        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = await _priorityRepo.FindAsync(request.WorkspaceId, request.Priority.Trim());
            priorityId = priority?.Id;
        }
        if (!priorityId.HasValue)
        {
            var defaultPriority = await _priorityRepo.FindAsync(request.WorkspaceId, "Normal");
            priorityId = defaultPriority?.Id;
        }

        int? statusId = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = await _statusRepo.FindByNameAsync(request.WorkspaceId, request.Status.Trim());
            statusId = status?.Id;
        }
        if (!statusId.HasValue)
        {
            var defaultStatus = await _statusRepo.FindByNameAsync(request.WorkspaceId, "New");
            statusId = defaultStatus?.Id;
        }

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

        // Validate and set user assignment
        if (request.AssignedUserId.HasValue)
        {
            if (await ValidateUserAssignmentAsync(request.AssignedUserId.Value, request.WorkspaceId))
            {
                ticket.AssignedUserId = request.AssignedUserId.Value;
            }
        }
        else if (request.LocationId.HasValue)
        {
            // Auto-assign from location default
            var defaultAssignee = await ResolveDefaultAssigneeAsync(request.LocationId.Value, request.WorkspaceId);
            if (defaultAssignee.HasValue)
            {
                ticket.AssignedUserId = defaultAssignee.Value;
            }
        }

        // Validate and set team assignment
        if (request.AssignedTeamId.HasValue)
        {
            if (await ValidateTeamAssignmentAsync(request.AssignedTeamId.Value, request.WorkspaceId))
            {
                ticket.AssignedTeamId = request.AssignedTeamId.Value;
            }
        }

        await _ticketRepo.CreateAsync(ticket);

        // Create history entry
        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = request.WorkspaceId,
            TicketId = ticket.Id,
            CreatedByUserId = request.CreatedByUserId,
            Action = "created",
            Note = "Ticket created"
        });

        return ticket;
    }

    public async Task<Ticket> UpdateTicketAsync(UpdateTicketRequest request)
    {
        var ticket = await _ticketRepo.FindAsync(request.WorkspaceId, request.TicketId);
        if (ticket == null)
            throw new InvalidOperationException("Ticket not found");

        // Capture old values for change tracking
        var oldSubject = ticket.Subject;
        var oldDescription = ticket.Description;
        var oldTypeId = ticket.TicketTypeId;
        var oldPriorityId = ticket.PriorityId;
        var oldStatusId = ticket.StatusId;
        var oldContactId = ticket.ContactId;
        var oldAssignedUserId = ticket.AssignedUserId;
        var oldLocationId = ticket.LocationId;
        var oldInventories = ticket.TicketInventories?.ToList() ?? new List<TicketInventory>();

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Subject))
            ticket.Subject = request.Subject.Trim();

        if (!string.IsNullOrWhiteSpace(request.Description))
            ticket.Description = request.Description.Trim();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = await _typeRepo.FindByNameAsync(request.WorkspaceId, request.Type.Trim());
            if (type != null)
                ticket.TicketTypeId = type.Id;
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = await _priorityRepo.FindAsync(request.WorkspaceId, request.Priority.Trim());
            if (priority != null)
                ticket.PriorityId = priority.Id;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = await _statusRepo.FindByNameAsync(request.WorkspaceId, request.Status.Trim());
            if (status != null)
                ticket.StatusId = status.Id;
        }

        if (request.ContactId.HasValue)
            ticket.ContactId = request.ContactId.Value;

        if (request.LocationId.HasValue)
            ticket.LocationId = request.LocationId.Value;

        // Update assignment
        if (request.AssignedUserId.HasValue)
        {
            if (await ValidateUserAssignmentAsync(request.AssignedUserId.Value, request.WorkspaceId))
            {
                ticket.AssignedUserId = request.AssignedUserId.Value;
            }
        }

        if (request.AssignedTeamId.HasValue)
        {
            if (await ValidateTeamAssignmentAsync(request.AssignedTeamId.Value, request.WorkspaceId))
            {
                ticket.AssignedTeamId = request.AssignedTeamId.Value;
            }
        }

        // Update inventories if provided
        if (request.Inventories != null)
        {
            ticket.TicketInventories = request.Inventories;
        }

        await _ticketRepo.UpdateAsync(ticket);

        // Log changes to history
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "Subject", oldSubject, ticket.Subject);
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "Description", oldDescription, ticket.Description);
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "TicketTypeId", oldTypeId?.ToString(), ticket.TicketTypeId?.ToString());
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "PriorityId", oldPriorityId?.ToString(), ticket.PriorityId?.ToString());
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "StatusId", oldStatusId?.ToString(), ticket.StatusId?.ToString());
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "ContactId", oldContactId?.ToString(), ticket.ContactId?.ToString());
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "AssignedUserId", oldAssignedUserId?.ToString(), ticket.AssignedUserId?.ToString());
        await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
            "LocationId", oldLocationId?.ToString(), ticket.LocationId?.ToString());

        // Log inventory changes
        if (request.Inventories != null)
        {
            var oldSummary = await GenerateInventorySummaryForHistoryAsync(oldInventories, request.WorkspaceId);
            var newSummary = await GenerateInventorySummaryForHistoryAsync(
                ticket.TicketInventories?.ToList() ?? new List<TicketInventory>(), 
                request.WorkspaceId);
            await LogFieldChangeAsync(request.WorkspaceId, ticket.Id, request.UpdatedByUserId, 
                "Inventory", oldSummary, newSummary);
        }

        return ticket;
    }

    public async Task<bool> ValidateUserAssignmentAsync(int userId, int workspaceId)
    {
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        return memberships.Any(m => m.UserId == userId && m.Accepted);
    }

    public async Task<bool> ValidateTeamAssignmentAsync(int teamId, int workspaceId)
    {
        var team = await _teamRepo.FindByIdAsync(teamId);
        return team != null && team.WorkspaceId == workspaceId;
    }

    public async Task<int?> ResolveDefaultAssigneeAsync(int locationId, int workspaceId)
    {
        var location = await _locationRepo.FindAsync(workspaceId, locationId);
        if (location?.DefaultAssigneeUserId == null)
            return null;

        // Verify user is in workspace
        if (await ValidateUserAssignmentAsync(location.DefaultAssigneeUserId.Value, workspaceId))
        {
            return location.DefaultAssigneeUserId.Value;
        }

        return null;
    }

    public async Task<bool> CanUserAccessTicketAsync(Ticket ticket, int userId, int workspaceId, bool isAdmin)
    {
        if (isAdmin)
            return true;

        var scope = await _rolePermRepo.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);

        switch (scope?.ToLowerInvariant())
        {
            case "all":
                return true;
            case "mine":
                return ticket.AssignedUserId == userId;
            case "team":
                if (ticket.AssignedUserId == userId)
                    return true;
                if (ticket.AssignedTeamId.HasValue)
                {
                    var myTeams = await _teamMemberRepo.ListTeamsForUserAsync(workspaceId, userId);
                    return myTeams.Any(t => t.Id == ticket.AssignedTeamId.Value);
                }
                return false;
            default:
                return false;
        }
    }

    public async Task<string?> GetAssigneeDisplayNameAsync(int userId)
    {
        var user = await _userRepo.FindByIdAsync(userId);
        if (user == null)
            return null;

        var name = user.Name?.Trim() ?? "(unknown)";
        var email = user.Email?.Trim() ?? string.Empty;

        return string.IsNullOrEmpty(email) ? name : $"{name} ({email})";
    }

    public async Task<(string summary, string details)> GenerateInventorySummaryAsync(
        List<TicketInventory> inventories, 
        int workspaceId)
    {
        if (inventories == null || inventories.Count == 0)
            return ("—", string.Empty);

        var count = inventories.Count;
        var total = inventories.Sum(iv => iv.UnitPrice * iv.Quantity);
        var summary = $"{count} item{(count == 1 ? string.Empty : "s")} · ${total:F2}";

        var detailParts = new List<string>();
        foreach (var iv in inventories)
        {
            var name = iv.Inventory?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                var inv = await _inventoryRepo.FindAsync(workspaceId, iv.InventoryId);
                name = inv?.Name ?? $"Item #{iv.InventoryId}";
            }
            detailParts.Add($"{name} x{iv.Quantity}");
        }

        return (summary, string.Join(", ", detailParts));
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
            return;

        await _historyRepo.CreateAsync(new TicketHistory
        {
            WorkspaceId = workspaceId,
            TicketId = ticketId,
            CreatedByUserId = userId,
            Action = "field_changed",
            Field = field,
            OldValue = string.IsNullOrEmpty(oldTrim) ? null : oldTrim,
            NewValue = string.IsNullOrEmpty(newTrim) ? null : newTrim
        });
    }

    private async Task<string> GenerateInventorySummaryForHistoryAsync(
        List<TicketInventory> inventories, 
        int workspaceId)
    {
        if (inventories == null || inventories.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var iv in inventories)
        {
            var name = iv.Inventory?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                var inv = await _inventoryRepo.FindAsync(workspaceId, iv.InventoryId);
                name = inv?.Name ?? $"Item #{iv.InventoryId}";
            }
            parts.Add($"{name} x{iv.Quantity} @ ${iv.UnitPrice:F2}");
        }

        return string.Join(", ", parts);
    }

    private static string DefaultOrTrim(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value!.Trim();
    }
}


