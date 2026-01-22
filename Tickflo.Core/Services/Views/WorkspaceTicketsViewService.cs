namespace Tickflo.Core.Services.Views;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Implementation of tickets view service.
/// Aggregates tickets metadata and permissions for list display.
/// </summary>
public class WorkspaceTicketsViewService(
    ITicketRepository ticketRepository,
    ITicketStatusRepository statusRepository,
    ITicketPriorityRepository priorityRepository,
    ITicketTypeRepository ticketTypeRepository,
    ITeamRepository teamRepository,
    IContactRepository contactRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserRepository userRepository,
    ILocationRepository locationRepository,
    IRolePermissionRepository rolePermissionRepository,
    ITeamMemberRepository teamMemberRepo,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository) : IWorkspaceTicketsViewService
{
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;
    private readonly ITicketTypeRepository ticketTypeRepository = ticketTypeRepository;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly IContactRepository contactRepository = contactRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly ILocationRepository locationRepository = locationRepository;
    private readonly IRolePermissionRepository rolePermissionRepository = rolePermissionRepository;
    private readonly ITeamMemberRepository teamMemberRepository = teamMemberRepo;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;

    public async Task<WorkspaceTicketsViewData> BuildAsync(
        int workspaceId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Determine if user is admin
        var isAdmin = userId > 0 && await this.userWorkspaceRoleRepository.IsAdminAsync(userId, workspaceId);
        var data = new WorkspaceTicketsViewData();

        // Load statuses with fallback defaults
        var statuses = await this.statusRepository.ListAsync(workspaceId, cancellationToken);
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
        var priorities = await this.priorityRepository.ListAsync(workspaceId, cancellationToken);
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
        var types = await this.ticketTypeRepository.ListAsync(workspaceId, cancellationToken);
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
        var teams = await this.teamRepository.ListForWorkspaceAsync(workspaceId);
        data.TeamsById = teams.ToDictionary(t => t.Id, t => t);

        // Load contacts
        var contacts = await this.contactRepository.ListAsync(workspaceId, cancellationToken);
        data.ContactsById = contacts.ToDictionary(c => c.Id, c => c);

        // Load workspace members
        var memberships = await this.userWorkspaceRepository.FindForWorkspaceAsync(workspaceId);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        var users = new List<User>();
        foreach (var id in userIds)
        {
            var user = await this.userRepository.FindByIdAsync(id);
            if (user != null)
            {
                users.Add(user);
            }
        }
        data.UsersById = users.ToDictionary(u => u.Id, u => u);

        // Load locations
        var locations = await this.locationRepository.ListAsync(workspaceId);
        data.LocationOptions = [.. locations];
        data.LocationsById = locations.ToDictionary(l => l.Id, l => l);

        // Determine permissions
        if (userId > 0)
        {
            var permissions = await this.rolePermissionRepository.GetEffectivePermissionsForUserAsync(workspaceId, userId);
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
        var scope = await this.rolePermissionRepository.GetTicketViewScopeForUserAsync(workspaceId, userId, isAdmin);
        data.TicketViewScope = scope;
        if (scope == "team" && userId > 0)
        {
            var userTeams = await this.teamMemberRepository.ListTeamsForUserAsync(workspaceId, userId);
            data.UserTeamIds = [.. userTeams.Select(t => t.Id)];
        }

        return data;
    }

    public async Task<IEnumerable<Ticket>> GetAllTicketsAsync(int workspaceId, CancellationToken cancellationToken = default) => await this.ticketRepository.ListAsync(workspaceId, cancellationToken);

    public async Task<Ticket?> GetTicketAsync(int workspaceId, int ticketId, CancellationToken cancellationToken = default) => await this.ticketRepository.FindAsync(workspaceId, ticketId, cancellationToken);
}


