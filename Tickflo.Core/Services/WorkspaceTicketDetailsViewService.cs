using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Implementation of ticket details view service.
/// Aggregates metadata, permissions, and scope enforcement.
/// </summary>
public class WorkspaceTicketDetailsViewService : IWorkspaceTicketDetailsViewService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IContactRepository _contactRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly ITeamMemberRepository _teamMemberRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IRolePermissionRepository _rolePermissionRepo;

    public WorkspaceTicketDetailsViewService(
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
        IRolePermissionRepository rolePermissionRepo)
    {
        _ticketRepo = ticketRepo;
        _contactRepo = contactRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _historyRepo = historyRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _teamRepo = teamRepo;
        _teamMemberRepo = teamMemberRepo;
        _inventoryRepo = inventoryRepo;
        _locationRepo = locationRepo;
        _rolePermissionRepo = rolePermissionRepo;
    }

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
            var perms = await _rolePermissionRepo.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (perms.TryGetValue("tickets", out var tp))
            {
                data.CanViewTickets = tp.CanView;
                data.CanEditTickets = tp.CanEdit;
                data.CanCreateTickets = tp.CanCreate;
                data.TicketViewScope = string.IsNullOrWhiteSpace(tp.TicketViewScope) ? "all" : tp.TicketViewScope;
            }
        }

        // Check admin status
        data.IsWorkspaceAdmin = userId > 0 && await _userWorkspaceRoleRepo.IsAdminAsync(userId, workspaceId);
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
            if (!data.IsWorkspaceAdmin && !data.CanViewTickets) return null;
        }
        else
        {
            if (!data.IsWorkspaceAdmin && !data.CanCreateTickets) return null;
        }

        // Load ticket (if exists)
        if (ticketId > 0)
        {
            data.Ticket = await _ticketRepo.FindAsync(workspaceId, ticketId);
            if (data.Ticket == null) return null;

            // Enforce scope for details
            if (!data.IsWorkspaceAdmin && userId > 0)
            {
                var scope = data.TicketViewScope.ToLowerInvariant();
                if (scope == "mine")
                {
                    if (data.Ticket.AssignedUserId != userId) return null;
                }
                else if (scope == "team")
                {
                    var myTeams = await _teamMemberRepo.ListTeamsForUserAsync(workspaceId, userId);
                    var teamIds = myTeams.Select(t => t.Id).ToHashSet();
                    var inScope = (data.Ticket.AssignedUserId == userId) || 
                        (data.Ticket.AssignedTeamId.HasValue && teamIds.Contains(data.Ticket.AssignedTeamId.Value));
                    if (!inScope) return null;
                }
            }

            // Load history for existing ticket
            data.History = await _historyRepo.ListForTicketAsync(workspaceId, ticketId);
        }
        else
        {
            // Create new ticket with defaults
            data.Ticket = new Ticket
            {
                WorkspaceId = workspaceId,
                Type = "Standard",
                Priority = "Normal",
                Status = "New",
                LocationId = locationId
            };
        }

        // Load contact if assigned
        if (data.Ticket.ContactId.HasValue)
        {
            data.Contact = await _contactRepo.FindAsync(workspaceId, data.Ticket.ContactId.Value);
        }

        // Load all contacts for selection
        data.Contacts = await _contactRepo.ListAsync(workspaceId, cancellationToken);

        // Load inventory items
        data.InventoryItems = (await _inventoryRepo.ListAsync(workspaceId, null, "active")).ToList();

        // Load statuses with fallback defaults
        var statuses = await _statusRepo.ListAsync(workspaceId, cancellationToken);
        var statusList = statuses.Count > 0
            ? statuses
            : new List<TicketStatus>
            {
                new() { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new() { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new() { WorkspaceId = workspaceId, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
            };
        data.Statuses = statusList;
        data.StatusColorByName = statusList
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load priorities with fallback defaults
        var priorities = await _priorityRepo.ListAsync(workspaceId, cancellationToken);
        var priorityList = priorities.Count > 0
            ? priorities
            : new List<TicketPriority>
            {
                new() { WorkspaceId = workspaceId, Name = "Low", Color = "warning", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "High", Color = "error", SortOrder = 3 },
            };
        data.Priorities = priorityList;
        data.PriorityColorByName = priorityList
            .GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load types with fallback defaults
        var types = await _typeRepo.ListAsync(workspaceId, cancellationToken);
        var typeList = types.Count > 0
            ? types
            : new List<TicketType>
            {
                new() { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new() { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new() { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 },
            };
        data.Types = typeList;
        data.TypeColorByName = typeList
            .GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load members
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var uid in userIds)
        {
            var user = await _userRepo.FindByIdAsync(uid);
            if (user != null) data.Members.Add(user);
        }

        // Load teams
        data.Teams = await _teamRepo.ListForWorkspaceAsync(workspaceId);

        // Load locations
        data.LocationOptions = (await _locationRepo.ListAsync(workspaceId)).ToList();
        if (data.Ticket != null && data.Ticket.Id > 0 && data.Ticket.LocationId.HasValue)
        {
            locationId = data.Ticket.LocationId;
        }

        return data;
    }
}
