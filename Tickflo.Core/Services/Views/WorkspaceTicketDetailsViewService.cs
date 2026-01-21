namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of ticket details view service.
/// Aggregates metadata, permissions, and scope enforcement.
/// </summary>
public class WorkspaceTicketDetailsViewService(
    ITicketRepository ticketRepo,
    IContactRepository contactRepo,
    ITicketStatusRepository statusRepo,
    ITicketPriorityRepository priorityRepo,
    ITicketTypeRepository typeRepo,
    ITicketHistoryRepository historyRepo,
    IUserRepository userRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    ITeamRepository teamRepo,
    ITeamMemberRepository teamMemberRepo,
    IInventoryRepository inventoryRepo,
    ILocationRepository locationRepo,
    IRolePermissionRepository rolePermissionRepo) : IWorkspaceTicketDetailsViewService
{
    private readonly ITicketRepository _ticketRepo = ticketRepo;
    private readonly IContactRepository _contactRepo = contactRepo;
    private readonly ITicketStatusRepository _statusRepo = statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;
    private readonly ITicketTypeRepository _typeRepo = typeRepo;
    private readonly ITicketHistoryRepository _historyRepo = historyRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly ITeamRepository _teamRepo = teamRepo;
    private readonly ITeamMemberRepository _teamMemberRepo = teamMemberRepo;
    private readonly IInventoryRepository _inventoryRepo = inventoryRepo;
    private readonly ILocationRepository _locationRepo = locationRepo;
    private readonly IRolePermissionRepository _rolePermissionRepo = rolePermissionRepo;

    public async Task<WorkspaceTicketDetailsViewData?> BuildAsync(
        int workspaceId,
        int ticketId,
        int userId,
        int? locationId,
        CancellationToken cancellationToken = default)
    {
        var data = new WorkspaceTicketDetailsViewData();

        // Load effective permissions
        if (userId > 0)
        {
            var perms = await this._rolePermissionRepo.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (perms.TryGetValue("tickets", out var tp))
            {
                data.CanViewTickets = tp.CanView;
                data.CanEditTickets = tp.CanEdit;
                data.CanCreateTickets = tp.CanCreate;
                data.TicketViewScope = string.IsNullOrWhiteSpace(tp.TicketViewScope) ? "all" : tp.TicketViewScope;
            }
        }

        // Check admin status
        data.IsWorkspaceAdmin = userId > 0 && await this._userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
        if (data.IsWorkspaceAdmin)
        {
            data.CanViewTickets = true;
            data.CanEditTickets = true;
            data.CanCreateTickets = true;
            data.TicketViewScope = "all";
        }

        // Enforce view/create permission before loading details
        if (ticketId > 0)
        {
            if (!data.IsWorkspaceAdmin && !data.CanViewTickets)
            {
                return null;
            }
        }
        else
        {
            if (!data.IsWorkspaceAdmin && !data.CanCreateTickets)
            {
                return null;
            }
        }

        // Load ticket (if exists)
        if (ticketId > 0)
        {
            data.Ticket = await this._ticketRepo.FindAsync(workspaceId, ticketId, cancellationToken);
            if (data.Ticket == null)
            {
                return null;
            }

            // Enforce scope for details
            if (!data.IsWorkspaceAdmin && userId > 0)
            {
                var scope = data.TicketViewScope.ToLowerInvariant();
                if (scope == "mine")
                {
                    if (data.Ticket.AssignedUserId != userId)
                    {
                        return null;
                    }
                }
                else if (scope == "team")
                {
                    var myTeams = await this._teamMemberRepo.ListTeamsForUserAsync(workspaceId, userId);
                    var teamIds = myTeams.Select(t => t.Id).ToHashSet();
                    var inScope = (data.Ticket.AssignedUserId == userId) ||
                        (data.Ticket.AssignedTeamId.HasValue && teamIds.Contains(data.Ticket.AssignedTeamId.Value));
                    if (!inScope)
                    {
                        return null;
                    }
                }
            }

            // Load history for existing ticket
            data.History = await this._historyRepo.ListForTicketAsync(workspaceId, ticketId);
        }
        else
        {
            // Create new ticket with defaults
            // Load default IDs from database
            var defaultType = await this._typeRepo.FindByNameAsync(workspaceId, "Standard", cancellationToken);
            var defaultPriority = await this._priorityRepo.FindAsync(workspaceId, "Normal", cancellationToken);
            var defaultStatus = await this._statusRepo.FindByNameAsync(workspaceId, "New", cancellationToken);

            data.Ticket = new Ticket
            {
                WorkspaceId = workspaceId,
                TicketTypeId = defaultType?.Id,
                PriorityId = defaultPriority?.Id,
                StatusId = defaultStatus?.Id,
                LocationId = locationId
            };
        }

        // Load contact if assigned
        if (data.Ticket.ContactId.HasValue)
        {
            data.Contact = await this._contactRepo.FindAsync(workspaceId, data.Ticket.ContactId.Value, cancellationToken);
        }

        // Load all contacts for selection
        data.Contacts = await this._contactRepo.ListAsync(workspaceId, cancellationToken);

        // Load inventory items
        data.InventoryItems = [.. await this._inventoryRepo.ListAsync(workspaceId, null, "active")];

        // Load statuses with fallback defaults
        var statuses = await this._statusRepo.ListAsync(workspaceId, cancellationToken);
        var statusList = statuses.Count > 0
            ? statuses
            :
            [
                new() { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new() { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new() { WorkspaceId = workspaceId, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
            ];
        data.Statuses = statusList;
        data.StatusColorByName = statusList
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load priorities with fallback defaults
        var priorities = await this._priorityRepo.ListAsync(workspaceId, cancellationToken);
        var priorityList = priorities.Count > 0
            ? priorities
            :
            [
                new() { WorkspaceId = workspaceId, Name = "Low", Color = "warning", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "High", Color = "error", SortOrder = 3 },
            ];
        data.Priorities = priorityList;
        data.PriorityColorByName = priorityList
            .GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load types with fallback defaults
        var types = await this._typeRepo.ListAsync(workspaceId, cancellationToken);
        var typeList = types.Count > 0
            ? types
            :
            [
                new() { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 },
            ];
        data.Types = typeList;
        data.TypeColorByName = typeList
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load members
        var memberships = await this._userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var uid in userIds)
        {
            var user = await this._userRepo.FindByIdAsync(uid);
            if (user != null)
            {
                data.Members.Add(user);
            }
        }

        // Load teams
        data.Teams = await this._teamRepo.ListForWorkspaceAsync(workspaceId);

        // Load locations
        data.LocationOptions = [.. await this._locationRepo.ListAsync(workspaceId)];
        if (data.Ticket != null && data.Ticket.Id > 0 && data.Ticket.LocationId.HasValue)
        {
            locationId = data.Ticket.LocationId;
        }

        return data;
    }
}


