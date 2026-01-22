namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Views;

[Authorize]
public class TicketsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepository, ITicketRepository ticketRepository, ITicketFilterService filterService, IWorkspaceTicketsViewService viewService, INotificationTriggerService notificationTriggerService) : WorkspacePageModel
{
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 200;
    private const int MinPageNumber = 1;
    private const string OpenStatusFilter = "Open";

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketFilterService _filterService = filterService;
    private readonly IWorkspaceTicketsViewService _viewService = viewService;
    private readonly INotificationTriggerService notificationTriggerService = notificationTriggerService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public IReadOnlyList<Ticket> Tickets { get; private set; } = [];
    public Dictionary<int, Contact> ContactsById { get; private set; } = [];
    public Dictionary<int, User> UsersById { get; private set; } = [];

    private string? _ticketViewScope;
    private List<int> _userTeamIds = [];

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
    public IReadOnlyList<TicketStatus> Statuses { get; private set; } = [];
    public Dictionary<string, string> StatusColorByName { get; private set; } = [];
    public IReadOnlyList<TicketPriority> PrioritiesList { get; private set; } = [];
    public Dictionary<string, string> PriorityColorByName { get; private set; } = [];
    public bool CanCreateTickets { get; private set; }
    public bool CanEditTickets { get; private set; }
    public IReadOnlyList<TicketType> TypesList { get; private set; } = [];
    public Dictionary<string, string> TypeColorByName { get; private set; } = [];
    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }
    public Dictionary<int, Team> TeamsById { get; private set; } = [];
    [BindProperty(SupportsGet = true)]
    public string? AssigneeTeamName { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? LocationId { get; set; }
    public List<Location> LocationOptions { get; private set; } = [];
    public Dictionary<int, Location> LocationsById { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;

        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        await this.LoadViewDataAsync(this.Workspace.Id, currentUserId);

        var allTickets = await this.ticketRepository.ListAsync(this.Workspace.Id);
        var scopedTickets = this._filterService.ApplyScopeFilter(allTickets, currentUserId,
            this._ticketViewScope ?? string.Empty,
            this._userTeamIds);

        var filteredTickets = await this.ApplyAllFiltersAsync(scopedTickets, allTickets);
        await this.PaginateResultsAsync(filteredTickets, currentUserId, allTickets);

        return this.Page();
    }

    private async Task LoadViewDataAsync(int workspaceId, int currentUserId)
    {
        var viewData = await this._viewService.BuildAsync(workspaceId, currentUserId);

        this.Statuses = viewData.Statuses;
        this.StatusColorByName = viewData.StatusColorByName;
        this.PrioritiesList = viewData.Priorities;
        this.PriorityColorByName = viewData.PriorityColorByName;
        this.TypesList = viewData.Types;
        this.TypeColorByName = viewData.TypeColorByName;
        this.TeamsById = viewData.TeamsById;
        this.ContactsById = viewData.ContactsById;
        this.UsersById = viewData.UsersById;
        this.LocationOptions = viewData.LocationOptions;
        this.LocationsById = viewData.LocationsById;
        this.CanCreateTickets = viewData.CanCreateTickets;
        this.CanEditTickets = viewData.CanEditTickets;

        this._ticketViewScope = viewData.TicketViewScope;
        this._userTeamIds = viewData.UserTeamIds;
    }

    private async Task<List<Ticket>> ApplyAllFiltersAsync(IEnumerable<Ticket> tickets, IEnumerable<Ticket> allTickets)
    {
        var criteria = this.BuildFilterCriteria();
        var filtered = this._filterService.ApplyFilters(tickets, criteria).ToList();

        filtered = this.ApplyStatusOpenFilter(filtered);
        filtered = this.ApplyContactFilter(filtered);
        filtered = this.ApplyTeamFilter(filtered);

        return filtered;
    }

    private TicketFilterCriteria BuildFilterCriteria() => new()
    {
        Query = this.Query?.Trim(),
        StatusId = this.ResolveStatusId(),
        PriorityId = this.ResolvePriorityId(),
        TypeId = this.ResolveTypeId(),
        AssigneeUserId = this.AssigneeUserId,
        LocationId = this.LocationId,
        Mine = this.Mine,
        CurrentUserId = this.ExtractCurrentUserId()
    };

    private int ExtractCurrentUserId() => this.TryGetUserId(out var userId) ? userId : 0;

    private int? ResolveTypeId()
    {
        if (string.IsNullOrWhiteSpace(this.Type))
        {
            return null;
        }

        return this.TypesList.FirstOrDefault(t => t.Name.Equals(this.Type.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private int? ResolveStatusId()
    {
        if (string.IsNullOrWhiteSpace(this.Status) || this.Status.Equals(OpenStatusFilter, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return this.Statuses.FirstOrDefault(s => s.Name.Equals(this.Status.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private int? ResolvePriorityId()
    {
        if (string.IsNullOrWhiteSpace(this.Priority))
        {
            return null;
        }

        return this.PrioritiesList.FirstOrDefault(p => p.Name.Equals(this.Priority.Trim(), StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private List<Ticket> ApplyStatusOpenFilter(List<Ticket> tickets)
    {
        if (string.IsNullOrWhiteSpace(this.Status) || !this.Status.Equals(OpenStatusFilter, StringComparison.OrdinalIgnoreCase))
        {
            return tickets;
        }

        var closedStatusIds = this.Statuses
            .Where(s => s.IsClosedState)
            .Select(s => s.Id)
            .ToHashSet();

        return [.. tickets.Where(t => !t.StatusId.HasValue || !closedStatusIds.Contains(t.StatusId.Value))];
    }

    private List<Ticket> ApplyContactFilter(List<Ticket> tickets)
    {
        if (string.IsNullOrWhiteSpace(this.ContactQuery))
        {
            return tickets;
        }

        var query = this.ContactQuery.Trim();
        return [.. tickets.Where(t => this.TicketMatchesContactQuery(t, query))];
    }

    private bool TicketMatchesContactQuery(Ticket ticket, string query)
    {
        if (!ticket.ContactId.HasValue || !this.ContactsById.TryGetValue(ticket.ContactId.Value, out var contact))
        {
            return false;
        }

        return (contact.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (contact.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private List<Ticket> ApplyTeamFilter(List<Ticket> tickets)
    {
        if (string.IsNullOrWhiteSpace(this.AssigneeTeamName))
        {
            return tickets;
        }

        var team = this.TeamsById.Values.FirstOrDefault(t =>
            string.Equals(t.Name, this.AssigneeTeamName.Trim(), StringComparison.OrdinalIgnoreCase));

        return team != null
            ? [.. tickets.Where(t => t.AssignedTeamId == team.Id)]
            : [];
    }

    private async Task PaginateResultsAsync(List<Ticket> filteredTickets, int currentUserId, IEnumerable<Ticket> allTickets)
    {
        this.MyCount = currentUserId > 0 ? this._filterService.CountMyTickets(allTickets, currentUserId) : 0;
        this.Total = filteredTickets.Count;

        var pageSize = NormalizePageSize(this.PageSize);
        var pageNumber = NormalizePageNumber(this.PageNumber);

        var startIndex = (pageNumber - 1) * pageSize;
        this.Tickets = [.. filteredTickets.Skip(startIndex).Take(pageSize)];
    }

    private static int NormalizePageSize(int pageSize) => pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

    private static int NormalizePageNumber(int pageNumber) => pageNumber <= 0 ? MinPageNumber : pageNumber;

    public async Task<IActionResult> OnPostAssignAsync(string slug, int id, int? assignedUserId)
    {
        this.WorkspaceSlug = slug;

        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;

        var ticket = await this.ticketRepository.FindAsync(this.Workspace!.Id, id);
        if (ticket == null)
        {
            return this.NotFound();
        }

        await this.UpdateTicketAssignmentAsync(ticket, assignedUserId, currentUserId);

        return this.RedirectToPage(new
        {
            slug,
            this.Query,
            this.Status,
            this.Priority,
            this.Type,
            this.ContactQuery,
            this.Mine,
            this.AssigneeUserId,
            this.AssigneeTeamName,
            this.LocationId,
            this.PageNumber,
            this.PageSize
        });
    }

    private async Task UpdateTicketAssignmentAsync(Ticket ticket, int? assignedUserId, int currentUserId)
    {
        var oldAssignedUserId = ticket.AssignedUserId;
        var newAssignedUserId = assignedUserId > 0 ? assignedUserId : null;

        if (oldAssignedUserId == newAssignedUserId)
        {
            return;
        }

        ticket.AssignedUserId = newAssignedUserId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await this.ticketRepository.UpdateAsync(ticket);

        await this.NotifyAssignmentChangeAsync(ticket, oldAssignedUserId, currentUserId);
    }

    private async Task NotifyAssignmentChangeAsync(Ticket ticket, int? oldAssignedUserId, int currentUserId) => await this.notificationTriggerService.NotifyTicketAssignmentChangedAsync(
            this.Workspace!.Id,
            ticket,
            oldAssignedUserId,
            null,
            currentUserId
        );
}
