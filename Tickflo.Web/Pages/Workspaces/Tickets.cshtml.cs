namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class TicketsModel(
    IWorkspaceService workspaceService,
    ITicketFilterService ticketFilterService,
    ITicketAssignmentService ticketAssignmentService,
    IWorkspaceTicketsViewService workspaceTicketsViewService,
    INotificationTriggerService notificationTriggerService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly ITicketFilterService ticketFilterService = ticketFilterService;
    private readonly ITicketAssignmentService ticketAssignmentService = ticketAssignmentService;
    private readonly IWorkspaceTicketsViewService workspaceTicketsViewService = workspaceTicketsViewService;
    private readonly INotificationTriggerService notificationTriggerService = notificationTriggerService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public IReadOnlyList<Ticket> Tickets { get; private set; } = [];
    public Dictionary<int, Contact> ContactsById { get; private set; } = [];
    public Dictionary<int, User> UsersById { get; private set; } = [];

    private string? ticketViewScope;
    private List<int> userTeamIds = [];

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

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var currentUserId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(currentUserId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        await this.LoadViewDataAsync(this.Workspace.Id, currentUserId);

        var filteredTickets = await this.FilterAndPaginateTicketsAsync(currentUserId);
        this.Tickets = filteredTickets;

        return this.Page();
    }

    private async Task LoadViewDataAsync(int workspaceId, int currentUserId)
    {
        var viewData = await this.workspaceTicketsViewService.BuildAsync(workspaceId, currentUserId);

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

        this.ticketViewScope = viewData.TicketViewScope;
        this.userTeamIds = viewData.UserTeamIds;
    }

    private async Task<List<Ticket>> FilterAndPaginateTicketsAsync(int currentUserId)
    {
        var allTickets = await this.workspaceTicketsViewService.GetAllTicketsAsync(this.Workspace!.Id);

        var scopedTickets = this.ticketFilterService.ApplyScopeFilter(
            allTickets,
            currentUserId,
            this.ticketViewScope ?? string.Empty,
            this.userTeamIds);

        var filtered = this.ApplyAllFilters(scopedTickets, currentUserId);

        this.MyCount = this.ticketFilterService.CountMyTickets(allTickets, currentUserId);
        this.Total = filtered.Count;

        return this.ticketFilterService.Paginate(filtered, this.PageNumber, this.PageSize);
    }

    private List<Ticket> ApplyAllFilters(List<Ticket> tickets, int currentUserId)
    {
        var criteria = new TicketFilterCriteria
        {
            Query = this.Query?.Trim(),
            StatusId = this.ticketFilterService.ResolveStatusId(this.Status, this.Statuses),
            PriorityId = this.ticketFilterService.ResolvePriorityId(this.Priority, this.PrioritiesList),
            TypeId = this.ticketFilterService.ResolveTypeId(this.Type, this.TypesList),
            AssigneeUserId = this.AssigneeUserId,
            LocationId = this.LocationId,
            Mine = this.Mine,
            CurrentUserId = currentUserId
        };

        var filtered = this.ticketFilterService.ApplyFilters(tickets, criteria);

        if (!string.IsNullOrWhiteSpace(this.Status) && this.Status.Equals(TicketFilterConstants.OpenStatusFilter, StringComparison.OrdinalIgnoreCase))
        {
            filtered = this.ticketFilterService.ApplyOpenStatusFilter(filtered, this.Statuses);
        }

        filtered = this.ticketFilterService.ApplyContactFilter(filtered, this.ContactQuery, this.ContactsById);
        filtered = this.ticketFilterService.ApplyTeamFilter(filtered, this.AssigneeTeamName, this.TeamsById);

        return filtered;
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug, int id, int? assignedUserId)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var currentUserId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(currentUserId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var ticket = await this.workspaceTicketsViewService.GetTicketAsync(this.Workspace.Id, id);
        if (ticket == null)
        {
            return this.NotFound();
        }

        var oldAssignedUserId = ticket.AssignedUserId;
        var assignmentChanged = await this.ticketAssignmentService.UpdateAssignmentAsync(ticket, assignedUserId, currentUserId);

        if (assignmentChanged)
        {
            await this.notificationTriggerService.NotifyTicketAssignmentChangedAsync(
                this.Workspace.Id,
                ticket,
                oldAssignedUserId,
                null,
                currentUserId);
        }

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
}
