namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of tickets view service.
/// Aggregates tickets metadata and permissions for list display.
/// </summary>
public class WorkspaceTicketsViewService(
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
    IUserWorkspaceRoleRepository uwr) : IWorkspaceTicketsViewService
{
    private readonly ITicketStatusRepository _statusRepo = statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo = priorityRepo;
    private readonly ITicketTypeRepository _typeRepo = typeRepo;
    private readonly ITeamRepository _teamRepo = teamRepo;
    private readonly IContactRepository _contactRepo = contactRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILocationRepository _locationRepo = locationRepo;
    private readonly IRolePermissionRepository _rolePermissionRepo = rolePermissionRepo;
    private readonly ITeamMemberRepository _teamMemberRepo = teamMemberRepo;
    private readonly IUserWorkspaceRoleRepository _uwr = uwr;

    public async Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Determine if user is admin
        var isAdmin = userId > 0 && await this._uwr.IsAdminAsync(userId, workspaceId);
        var data = new WorkspaceTicketsViewData();

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

        // Load teams
        var teams = await this._teamRepo.ListForWorkspaceAsync(workspaceId);
        data.TeamsById = teams.ToDictionary(t => t.Id, t => t);

        // Load contacts
        var contacts = await this._contactRepo.ListAsync(workspaceId, cancellationToken);
        data.ContactsById = contacts.ToDictionary(c => c.Id, c => c);

        // Load workspace members
        var memberships = await this._userWorkspaceRepo.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = new List<User>();
        foreach (var id in userIds)
        {
            var user = await this._userRepo.FindByIdAsync(id);
            if (user != null)
            {
                users.Add(user);
            }
        }
        data.UsersById = users.ToDictionary(u => u.Id, u => u);

        // Load locations
        var locations = await this._locationRepo.ListAsync(workspaceId);
        data.LocationOptions = [.. locations];
        data.LocationsById = locations.ToDictionary(l => l.Id, l => l);

        // Determine permissions
        if (userId > 0)
        {
            var permissions = await this._rolePermissionRepo.GetEffectivePermissionsForUserAsync(workspaceId, userId);
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
        var scope = await this._rolePermissionRepo.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
        data.TicketViewScope = scope;
        if (scope == "team" && userId > 0)
        {
            var userTeams = await this._teamMemberRepo.ListTeamsForUserAsync(workspaceId, userId);
            data.UserTeamIds = [.. userTeams.Select(t => t.Id)];
        }

        return data;
    }
}


