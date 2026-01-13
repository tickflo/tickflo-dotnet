using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TicketsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketFilterService _filterService;
    private readonly IWorkspaceTicketsViewService _viewService;
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

    public TicketsModel(IWorkspaceRepository workspaceRepo, ITicketRepository ticketRepo, ITicketFilterService filterService, IWorkspaceTicketsViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _ticketRepo = ticketRepo;
        _filterService = filterService;
        _viewService = viewService;
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
            // Get user ID
            var currentUserId = TryGetUserId(out var parsedId) ? parsedId : 0;

            // Load view data
            var viewData = await _viewService.BuildAsync(Workspace.Id, currentUserId);
            Statuses = viewData.Statuses;
            StatusColorByName = viewData.StatusColorByName;
            PrioritiesList = viewData.Priorities;
            PriorityColorByName = viewData.PriorityColorByName;
            TypesList = viewData.Types;
            TypeColorByName = viewData.TypeColorByName;
            TeamsById = viewData.TeamsById;
            ContactsById = viewData.ContactsById;
            UsersById = viewData.UsersById;
            LocationOptions = viewData.LocationOptions;
            LocationsById = viewData.LocationsById;
            CanCreateTickets = viewData.CanCreateTickets;
            CanEditTickets = viewData.CanEditTickets;

            // Load and filter tickets
            var all = await _ticketRepo.ListAsync(Workspace.Id);
            var scoped = _filterService.ApplyScopeFilter(all, currentUserId, viewData.TicketViewScope, viewData.UserTeamIds);

            // Apply filters
            var criteria = new TicketFilterCriteria
            {
                Query = Query?.Trim(),
                Status = Status?.Trim(),
                Priority = Priority?.Trim(),
                Type = Type?.Trim(),
                AssigneeUserId = AssigneeUserId,
                LocationId = LocationId,
                Mine = Mine,
                CurrentUserId = currentUserId
            };
            var filteredList = _filterService.ApplyFilters(scoped, criteria);

            // Special case: Status == "Open" means non-closed statuses
            if (!string.IsNullOrWhiteSpace(Status) && Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
            {
                var openStatusNames = Statuses.Where(s => !s.IsClosedState)
                    .Select(s => s.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                filteredList = filteredList.Where(t => openStatusNames.Contains(t.Status)).ToList();
            }

            // ContactQuery filter (name/email)
            if (!string.IsNullOrWhiteSpace(ContactQuery))
            {
                var cq = ContactQuery.Trim();
                filteredList = filteredList.Where(t =>
                    t.ContactId.HasValue && ContactsById.TryGetValue(t.ContactId.Value, out var c) && (
                        (c.Name?.Contains(cq, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.Email?.Contains(cq, StringComparison.OrdinalIgnoreCase) ?? false)
                    )
                ).ToList();
            }

            // Team name filter
            if (!string.IsNullOrWhiteSpace(AssigneeTeamName))
            {
                var team = TeamsById.Values.FirstOrDefault(t => string.Equals(t.Name, AssigneeTeamName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (team != null)
                {
                    filteredList = filteredList.Where(t => t.AssignedTeamId == team.Id).ToList();
                }
                else
                {
                    filteredList = new List<Ticket>();
                }
            }

            // Pagination
            MyCount = currentUserId > 0 ? _filterService.CountMyTickets(all, currentUserId) : 0;
            Total = filteredList.Count();
            var size = PageSize <= 0 ? 25 : Math.Min(PageSize, 200);
            var page = PageNumber <= 0 ? 1 : PageNumber;
            Tickets = filteredList.Skip((page - 1) * size).Take(size).ToList();
        }
    }
}

