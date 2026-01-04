using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using System.Linq;

namespace Tickflo.Web.Pages.Workspaces;

public class TicketsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserRepository _users;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly IHttpContextAccessor _http;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ITeamMemberRepository _teamMembers;
    private readonly IUserWorkspaceRoleRepository _uwr;
    private readonly ILocationRepository _locations;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();
    public Dictionary<int, Contact> ContactsById { get; private set; } = new();
    public Dictionary<int, User> UsersById { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? ContactQuery { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool Mine { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? AssigneeUserId { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;
    public int Total { get; private set; }
    public int MyCount { get; private set; }
    public IReadOnlyList<Tickflo.Core.Entities.TicketStatus> Statuses { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketStatus>();
    public Dictionary<string, string> StatusColorByName { get; private set; } = new();
    public IReadOnlyList<Tickflo.Core.Entities.TicketPriority> PrioritiesList { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketPriority>();
    public Dictionary<string, string> PriorityColorByName { get; private set; } = new();
    public bool CanCreateTickets { get; private set; }
    public bool CanEditTickets { get; private set; }

    public TicketsModel(IWorkspaceRepository workspaceRepo, ITicketRepository ticketRepo, IContactRepository contactRepo, IUserWorkspaceRepository userWorkspaces, IUserRepository users, IHttpContextAccessor http, ITicketStatusRepository statusRepo, ITicketPriorityRepository priorityRepo, ITicketTypeRepository typeRepo, ITeamRepository teamRepo, IRolePermissionRepository rolePerms, ITeamMemberRepository teamMembers, IUserWorkspaceRoleRepository uwr, ILocationRepository locations)
    {
        _workspaceRepo = workspaceRepo;
        _ticketRepo = ticketRepo;
        _contactRepo = contactRepo;
        _userWorkspaces = userWorkspaces;
        _users = users;
        _http = http;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _teamRepo = teamRepo;
        _rolePerms = rolePerms;
        _teamMembers = teamMembers;
        _uwr = uwr;
        _locations = locations;
    }
    public IReadOnlyList<Tickflo.Core.Entities.TicketType> TypesList { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketType>();
    public Dictionary<string, string> TypeColorByName { get; private set; } = new();
    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }
    public Dictionary<int, Team> TeamsById { get; private set; } = new();
    [BindProperty(SupportsGet = true)]
    public string? AssigneeTeamName { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? LocationId { get; set; }
    public List<Location> LocationOptions { get; private set; } = new();
    public Dictionary<int, Location> LocationsById { get; private set; } = new();

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null)
        {
            // Load statuses and build color map
            var sts = await _statusRepo.ListAsync(Workspace.Id);
            if (sts.Count == 0)
            {
                // Fallback defaults (in case settings page hasn't bootstrapped yet)
                sts = new List<Tickflo.Core.Entities.TicketStatus>{
                    new() { WorkspaceId = Workspace.Id, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                    new() { WorkspaceId = Workspace.Id, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                    new() { WorkspaceId = Workspace.Id, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
                };
            }
            Statuses = sts;
            StatusColorByName = sts
                .GroupBy(s => s.Name)
                .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

            // Load priorities and build color map
            var pris = await _priorityRepo.ListAsync(Workspace.Id);
            if (pris.Count == 0)
            {
                pris = new List<Tickflo.Core.Entities.TicketPriority>{
                    new() { WorkspaceId = Workspace.Id, Name = "Low", Color = "warning", SortOrder = 1 },
                    new() { WorkspaceId = Workspace.Id, Name = "Normal", Color = "neutral", SortOrder = 2 },
                    new() { WorkspaceId = Workspace.Id, Name = "High", Color = "error", SortOrder = 3 },
                };
            }
            PrioritiesList = pris;
            PriorityColorByName = pris.GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

            // Load types and build color map
            var types = await _typeRepo.ListAsync(Workspace.Id);
            if (types.Count == 0)
            {
                types = new List<Tickflo.Core.Entities.TicketType>{
                    new() { WorkspaceId = Workspace.Id, Name = "Standard", Color = "neutral", SortOrder = 1 },
                    new() { WorkspaceId = Workspace.Id, Name = "Bug", Color = "error", SortOrder = 2 },
                    new() { WorkspaceId = Workspace.Id, Name = "Feature", Color = "primary", SortOrder = 3 },
                };
            }
            TypesList = types;
            TypeColorByName = types.GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

            // Load teams for filtering/display
            var teams = await _teamRepo.ListForWorkspaceAsync(Workspace.Id);
            TeamsById = teams.ToDictionary(t => t.Id, t => t);

            var all = await _ticketRepo.ListAsync(Workspace.Id);
            // Apply role-based ticket scope (mine or my team)
            var uidStr2 = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = 0;
            bool isAdmin = false;
            if (int.TryParse(uidStr2, out var uid2))
            {
                currentUserId = uid2;
                isAdmin = await _uwr.IsAdminAsync(currentUserId, Workspace.Id);
            }
            // Compute edit/create permissions for UI actions
            if (currentUserId > 0)
            {
                var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, currentUserId);
                if (eff.TryGetValue("tickets", out var tp))
                {
                    CanCreateTickets = tp.CanCreate;
                    CanEditTickets = tp.CanEdit;
                }
                else
                {
                    CanCreateTickets = isAdmin;
                    CanEditTickets = isAdmin;
                }
            }
            // Note: repository provides scope given admin flag
            var scope = await _rolePerms.GetTicketViewScopeForUserAsync(Workspace.Id, currentUserId, isAdmin);
            var filtered = all.AsEnumerable();
            if (scope == "mine" && currentUserId > 0)
            {
                filtered = filtered.Where(t => t.AssignedUserId == currentUserId);
            }
            else if (scope == "team" && currentUserId > 0)
            {
                var myTeams = await _teamMembers.ListTeamsForUserAsync(Workspace.Id, currentUserId);
                var teamIds = myTeams.Select(t => t.Id).ToHashSet();
                filtered = filtered.Where(t => t.AssignedTeamId.HasValue && teamIds.Contains(t.AssignedTeamId.Value));
            }
            if (!string.IsNullOrWhiteSpace(Status))
            {
                if (Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
                {
                    // Get all status names that are NOT closed
                    var openStatusNames = Statuses.Where(s => !s.IsClosedState).Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    filtered = filtered.Where(t => openStatusNames.Contains(t.Status));
                }
                else
                {
                    filtered = filtered.Where(t => string.Equals(t.Status, Status, StringComparison.OrdinalIgnoreCase));
                }
            }
            if (!string.IsNullOrWhiteSpace(Priority)) filtered = filtered.Where(t => t.Priority == Priority);
                        if (!string.IsNullOrWhiteSpace(Type)) filtered = filtered.Where(t => t.Type == Type);
            if (!string.IsNullOrWhiteSpace(ContactQuery))
            {
                var cq = ContactQuery.Trim();
                filtered = filtered.Where(t =>
                    t.ContactId.HasValue && ContactsById.TryGetValue(t.ContactId.Value, out var c) && (
                        (c.Name?.Contains(cq, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Email?.Contains(cq, StringComparison.OrdinalIgnoreCase) ?? false)
                    )
                );
            }
            if (!string.IsNullOrWhiteSpace(Query))
            {
                var q = Query.Trim();
                filtered = filtered.Where(t => (t.Subject?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                                            || (t.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            if (LocationId.HasValue)
            {
                filtered = filtered.Where(t => t.LocationId == LocationId.Value);
            }
            var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int currentUid = 0;
            if (int.TryParse(uidStr, out var uidTmp)) currentUid = uidTmp;
            MyCount = currentUid > 0 ? all.Count(t => t.AssignedUserId == currentUid) : 0;

            if (Mine && currentUid > 0)
            {
                filtered = filtered.Where(t => t.AssignedUserId == currentUid);
            }
            if (AssigneeUserId.HasValue)
            {
                if (AssigneeUserId.Value == -1)
                {
                    filtered = filtered.Where(t => t.AssignedUserId == null);
                }
                else if (AssigneeUserId.Value > 0)
                {
                    filtered = filtered.Where(t => t.AssignedUserId == AssigneeUserId.Value);
                }
            }
            if (!string.IsNullOrWhiteSpace(AssigneeTeamName))
            {
                var team = teams.FirstOrDefault(t => string.Equals(t.Name, AssigneeTeamName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (team != null)
                {
                    filtered = filtered.Where(t => t.AssignedTeamId == team.Id);
                }
                else
                {
                    // If team does not exist, result should be empty
                    filtered = Enumerable.Empty<Ticket>();
                }
            }
            Total = filtered.Count();
            var size = PageSize <= 0 ? 25 : Math.Min(PageSize, 200);
            var page = PageNumber <= 0 ? 1 : PageNumber;
            Tickets = filtered.Skip((page - 1) * size).Take(size).ToList();

            // Load contacts for display (name/email)
            var contacts = await _contactRepo.ListAsync(Workspace.Id);
            ContactsById = contacts.ToDictionary(c => c.Id, c => c);

            // Load workspace members for assignee display/filter
            var memberships = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
            var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
            var users = new List<User>();
            foreach (var id in userIds)
            {
                var u = await _users.FindByIdAsync(id);
                if (u != null) users.Add(u);
            }
            UsersById = users.ToDictionary(u => u.Id, u => u);

            // TeamsById already set above
            LocationOptions = (await _locations.ListAsync(Workspace.Id)).ToList();
            LocationsById = LocationOptions.ToDictionary(l => l.Id, l => l);
        }
    }
}
