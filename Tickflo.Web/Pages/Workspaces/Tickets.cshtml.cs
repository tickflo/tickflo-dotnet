using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Notifications;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TicketsModel : WorkspacePageModel
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 200;
    private const int MinPageNumber = 1;
    private const string OpenStatusFilter = "Open";

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketFilterService _filterService;
    private readonly IWorkspaceTicketsViewService _viewService;
    private readonly INotificationTriggerService _notificationTrigger;

    public TicketsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, ITicketRepository ticketRepo, ITicketFilterService filterService, IWorkspaceTicketsViewService viewService, INotificationTriggerService notificationTrigger)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _ticketRepo = ticketRepo;
        _filterService = filterService;
        _viewService = viewService;
        _notificationTrigger = notificationTrigger;
    }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();
    public Dictionary<int, Contact> ContactsById { get; private set; } = new();
    public Dictionary<int, User> UsersById { get; private set; } = new();

    private string? _ticketViewScope;
    private List<int> _userTeamIds = new();

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

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) 
            return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        if (Workspace == null)
            return NotFound();

        await LoadViewDataAsync(Workspace.Id, currentUserId);

        var allTickets = await _ticketRepo.ListAsync(Workspace.Id);
        var scopedTickets = _filterService.ApplyScopeFilter(allTickets, currentUserId, 
            _ticketViewScope ?? string.Empty, 
            _userTeamIds);

        var filteredTickets = await ApplyAllFiltersAsync(scopedTickets, allTickets);
        await PaginateResultsAsync(filteredTickets, currentUserId, allTickets);
        
        return Page();
    }

    private async Task LoadViewDataAsync(int workspaceId, int currentUserId)
    {
        var viewData = await _viewService.BuildAsync(workspaceId, currentUserId);
        
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
        
        _ticketViewScope = viewData.TicketViewScope;
        _userTeamIds = viewData.UserTeamIds;
    }

    private async Task<List<Ticket>> ApplyAllFiltersAsync(IEnumerable<Ticket> tickets, IEnumerable<Ticket> allTickets)
    {
        var criteria = BuildFilterCriteria();
        var filtered = _filterService.ApplyFilters(tickets, criteria).ToList();

        filtered = ApplyStatusOpenFilter(filtered);
        filtered = ApplyContactFilter(filtered);
        filtered = ApplyTeamFilter(filtered);

        return filtered;
    }

    private TicketFilterCriteria BuildFilterCriteria()
    {
        return new TicketFilterCriteria
        {
            Query = Query?.Trim(),
            StatusId = ResolveStatusId(),
            PriorityId = ResolvePriorityId(),
            TypeId = ResolveTypeId(),
            AssigneeUserId = AssigneeUserId,
            LocationId = LocationId,
            Mine = Mine,
            CurrentUserId = ExtractCurrentUserId()
        };
    }

    private int ExtractCurrentUserId()
    {
        return TryGetUserId(out var userId) ? userId : 0;
    }

    private int? ResolveTypeId()
    {
        if (string.IsNullOrWhiteSpace(Type))
            return null;

        return TypesList.FirstOrDefault(t => t.Name.Equals(Type.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private int? ResolveStatusId()
    {
        if (string.IsNullOrWhiteSpace(Status) || Status.Equals(OpenStatusFilter, StringComparison.OrdinalIgnoreCase))
            return null;

        return Statuses.FirstOrDefault(s => s.Name.Equals(Status.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private int? ResolvePriorityId()
    {
        if (string.IsNullOrWhiteSpace(Priority))
            return null;

        return PrioritiesList.FirstOrDefault(p => p.Name.Equals(Priority.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private List<Ticket> ApplyStatusOpenFilter(List<Ticket> tickets)
    {
        if (string.IsNullOrWhiteSpace(Status) || !Status.Equals(OpenStatusFilter, StringComparison.OrdinalIgnoreCase))
            return tickets;

        var closedStatusIds = Statuses
            .Where(s => s.IsClosedState)
            .Select(s => s.Id)
            .ToHashSet();

        return tickets.Where(t => !t.StatusId.HasValue || !closedStatusIds.Contains(t.StatusId.Value)).ToList();
    }

    private List<Ticket> ApplyContactFilter(List<Ticket> tickets)
    {
        if (string.IsNullOrWhiteSpace(ContactQuery))
            return tickets;

        var query = ContactQuery.Trim();
        return tickets.Where(t => TicketMatchesContactQuery(t, query)).ToList();
    }

    private bool TicketMatchesContactQuery(Ticket ticket, string query)
    {
        if (!ticket.ContactId.HasValue || !ContactsById.TryGetValue(ticket.ContactId.Value, out var contact))
            return false;

        return (contact.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (contact.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private List<Ticket> ApplyTeamFilter(List<Ticket> tickets)
    {
        if (string.IsNullOrWhiteSpace(AssigneeTeamName))
            return tickets;

        var team = TeamsById.Values.FirstOrDefault(t => 
            string.Equals(t.Name, AssigneeTeamName.Trim(), StringComparison.OrdinalIgnoreCase));

        return team != null 
            ? tickets.Where(t => t.AssignedTeamId == team.Id).ToList()
            : new List<Ticket>();
    }

    private async Task PaginateResultsAsync(List<Ticket> filteredTickets, int currentUserId, IEnumerable<Ticket> allTickets)
    {
        MyCount = currentUserId > 0 ? _filterService.CountMyTickets(allTickets, currentUserId) : 0;
        Total = filteredTickets.Count;

        var pageSize = NormalizePageSize(PageSize);
        var pageNumber = NormalizePageNumber(PageNumber);

        var startIndex = (pageNumber - 1) * pageSize;
        Tickets = filteredTickets.Skip(startIndex).Take(pageSize).ToList();
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
    }

    private static int NormalizePageNumber(int pageNumber)
    {
        return pageNumber <= 0 ? MinPageNumber : pageNumber;
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug, int id, int? assignedUserId)
    {
        WorkspaceSlug = slug;
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) 
            return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var ticket = await _ticketRepo.FindAsync(Workspace!.Id, id);
        if (ticket == null)
            return NotFound();

        await UpdateTicketAssignmentAsync(ticket, assignedUserId, currentUserId);
        
        return RedirectToPage(new 
        { 
            slug, 
            Query, 
            Status, 
            Priority, 
            Type, 
            ContactQuery, 
            Mine, 
            AssigneeUserId, 
            AssigneeTeamName, 
            LocationId, 
            PageNumber, 
            PageSize 
        });
    }

    private async Task UpdateTicketAssignmentAsync(Ticket ticket, int? assignedUserId, int currentUserId)
    {
        var oldAssignedUserId = ticket.AssignedUserId;
        var newAssignedUserId = assignedUserId > 0 ? assignedUserId : null;

        if (oldAssignedUserId == newAssignedUserId)
            return;

        ticket.AssignedUserId = newAssignedUserId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _ticketRepo.UpdateAsync(ticket);

        await NotifyAssignmentChangeAsync(ticket, oldAssignedUserId, currentUserId);
    }

    private async Task NotifyAssignmentChangeAsync(Ticket ticket, int? oldAssignedUserId, int currentUserId)
    {
        await _notificationTrigger.NotifyTicketAssignmentChangedAsync(
            Workspace!.Id,
            ticket,
            oldAssignedUserId,
            null,
            currentUserId
        );
    }
}
