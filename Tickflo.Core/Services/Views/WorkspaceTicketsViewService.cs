using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Views;

/// <summary>
/// Implementation of tickets view service.
/// Aggregates tickets metadata and permissions for list display.
/// </summary>
public class WorkspaceTicketsViewService : IWorkspaceTicketsViewService
{
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IRolePermissionRepository _rolePermissionRepo;
    private readonly ITeamMemberRepository _teamMemberRepo;
    private readonly IUserWorkspaceRoleRepository _uwr;

    public WorkspaceTicketsViewService(
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo,
        ITeamRepository teamRepo,
        IContactRepository contactRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IUserRepository userRepo,
        ILocationRepository locationRepo,
        IRolePermissionRepository rolePermissionRepo,
        ITeamMemberRepository teamMemberRepo,
        IUserWorkspaceRoleRepository uwr)
    {
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _teamRepo = teamRepo;
        _contactRepo = contactRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userRepo = userRepo;
        _locationRepo = locationRepo;
        _rolePermissionRepo = rolePermissionRepo;
        _teamMemberRepo = teamMemberRepo;
        _uwr = uwr;
    }

    public async Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Determine if user is admin
        bool isAdmin = userId > 0 && await _uwr.IsAdminAsync(userId, workspaceId);
        var data = new WorkspaceTicketsViewData();

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

        // Load teams
        var teams = await _teamRepo.ListForWorkspaceAsync(workspaceId);
        data.TeamsById = teams.ToDictionary(t => t.Id, t => t);

        // Load contacts
        var contacts = await _contactRepo.ListAsync(workspaceId, cancellationToken);
        data.ContactsById = contacts.ToDictionary(c => c.Id, c => c);

        // Load workspace members
        var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = new List<User>();
        foreach (var id in userIds)
        {
            var user = await _userRepo.FindByIdAsync(id);
            if (user != null) users.Add(user);
        }
        data.UsersById = users.ToDictionary(u => u.Id, u => u);

        // Load locations
        var locations = await _locationRepo.ListAsync(workspaceId);
        data.LocationOptions = locations.ToList();
        data.LocationsById = locations.ToDictionary(l => l.Id, l => l);

        // Determine permissions
        if (userId > 0)
        {
            var permissions = await _rolePermissionRepo.GetEffectivePermissionsForUserAsync(workspaceId, userId);
            if (permissions.TryGetValue("tickets", out var ticketPerms))
            {
                data.CanCreateTickets = ticketPerms.CanCreate;
                data.CanEditTickets = ticketPerms.CanEdit;
            }
            else
            {
                data.CanCreateTickets = isAdmin;
                data.CanEditTickets = isAdmin;
            }
        }

        // Determine scope and team IDs
        var scope = await _rolePermissionRepo.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
        data.TicketViewScope = scope;
        if (scope == "team" && userId > 0)
        {
            var userTeams = await _teamMemberRepo.ListTeamsForUserAsync(workspaceId, userId);
            data.UserTeamIds = userTeams.Select(t => t.Id).ToList();
        }

        return data;
    }
}


