namespace Tickflo.Web.Pages.Workspaces;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Notifications;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class TicketsDetailsModel(IWorkspaceService workspaceService, ITicketRepository ticketRepository, ITicketManagementService ticketManagementService, IWorkspaceTicketDetailsViewService workspaceTicketDetailsViewService, ITeamRepository teamRepository, IWorkspaceTicketsSaveViewService workspaceTicketsSaveViewService, ITicketCommentService ticketCommentService, INotificationTriggerService notificationTriggerService) : WorkspacePageModel
{
    #region Constants
    private const string DefaultTicketType = "Standard";
    private const string DefaultTicketPriority = "Normal";
    private const string DefaultTicketStatus = "New";
    private const string SuccessTicketSaved = "Ticket saved.";
    private const string ErrorCommentEmpty = "Comment cannot be empty.";
    private const string ErrorTicketNotFound = "Ticket not found.";
    private const int InvalidTicketId = 0;
    #endregion

    [BindProperty]
    public string? TicketInventoriesJson { get; set; }

    private sealed class TicketInventoryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("sku")]
        public string Sku { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }
    }
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly ITicketRepository ticketRepository = ticketRepository;
    private readonly ITicketManagementService ticketManagementService = ticketManagementService;
    private readonly IWorkspaceTicketDetailsViewService workspaceTicketDetailsViewService = workspaceTicketDetailsViewService;
    private readonly ITeamRepository teamRepository = teamRepository;
    private readonly IWorkspaceTicketsSaveViewService workspaceTicketsSaveViewService = workspaceTicketsSaveViewService;
    private readonly ITicketCommentService ticketCommentService = ticketCommentService;
    private readonly INotificationTriggerService notificationTriggerService = notificationTriggerService;

    public List<Inventory> InventoryItems { get; private set; } = [];

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Ticket? Ticket { get; private set; }
    public Contact? Contact { get; private set; }
    public IReadOnlyList<Contact> Contacts { get; private set; } = [];
    public bool IsWorkspaceAdmin { get; private set; }
    public bool CanViewTickets { get; private set; }
    public bool CanEditTickets { get; private set; }
    public bool CanCreateTickets { get; private set; }
    public string? TicketViewScope { get; private set; }
    public List<User> Members { get; private set; } = [];
    public IReadOnlyList<TicketStatus> Statuses { get; private set; } = [];
    public Dictionary<string, string> StatusColorByName { get; private set; } = [];
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = [];
    public Dictionary<string, string> PriorityColorByName { get; private set; } = [];
    public IReadOnlyList<TicketType> Types { get; private set; } = [];
    public Dictionary<string, string> TypeColorByName { get; private set; } = [];
    public IReadOnlyList<TicketHistory> History { get; private set; } = [];
    public List<Team> Teams { get; private set; } = [];
    [BindProperty(SupportsGet = true)]
    public int? LocationId { get; set; }
    public List<Location> LocationOptions { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? Priority { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? ContactId { get; set; }
    [BindProperty(SupportsGet = true)]
    public bool Mine { get; set; }
    [BindProperty(SupportsGet = true)]
    public int? AssigneeUserId { get; set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;

    [BindProperty]
    public string? EditSubject { get; set; }
    [BindProperty]
    public string? EditDescription { get; set; }
    [BindProperty]
    public string? EditType { get; set; }
    [BindProperty]
    public string? EditPriority { get; set; }
    [BindProperty]
    public string? EditStatus { get; set; }
    [BindProperty]
    public string? EditInventoryRef { get; set; }
    [BindProperty]
    public int? EditContactId { get; set; }

    public IReadOnlyList<TicketComment> Comments { get; private set; } = [];
    [BindProperty]
    public string? NewCommentContent { get; set; }
    [BindProperty]
    public bool NewCommentIsVisibleToClient { get; set; } = false;

    public async Task<IActionResult> OnGetAsync(string slug, int id)
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

        var viewData = await this.workspaceTicketDetailsViewService.BuildAsync(this.Workspace.Id, id, currentUserId, this.LocationId);
        if (viewData == null)
        {
            return this.Forbid();
        }

        this.LoadViewDataFromService(viewData);
        this.LocationId = this.Ticket?.LocationId;

        if (this.Ticket != null && this.Ticket.Id > InvalidTicketId)
        {
            this.Comments = await this.ticketCommentService.GetCommentsAsync(this.Workspace.Id, this.Ticket.Id, isClientView: false);
        }

        return this.Page();
    }

    private void LoadViewDataFromService(WorkspaceTicketDetailsViewData viewData)
    {
        this.Ticket = viewData.Ticket;
        this.Contact = viewData.Contact;
        this.Contacts = viewData.Contacts;
        this.Statuses = viewData.Statuses;
        this.StatusColorByName = viewData.StatusColorByName;
        this.Priorities = viewData.Priorities;
        this.PriorityColorByName = viewData.PriorityColorByName;
        this.Types = viewData.Types;
        this.TypeColorByName = viewData.TypeColorByName;
        this.History = viewData.History;
        this.Members = viewData.Members;
        this.Teams = viewData.Teams;
        this.InventoryItems = viewData.InventoryItems;
        this.LocationOptions = viewData.LocationOptions;
        this.CanViewTickets = viewData.CanViewTickets;
        this.CanEditTickets = viewData.CanEditTickets;
        this.CanCreateTickets = viewData.CanCreateTickets;
        this.IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        this.TicketViewScope = viewData.TicketViewScope;
    }

    public async Task<IActionResult> OnPostAddCommentAsync(string slug, int id, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Realtime.TicketsHub> hub)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var currentUserId = this.ExtractCurrentUserId();
        if (currentUserId == InvalidTicketId)
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(currentUserId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        if (string.IsNullOrWhiteSpace(this.NewCommentContent))
        {
            this.SetErrorMessage(ErrorCommentEmpty);
            return this.RedirectToPage("/Workspaces/TicketsDetails", new { slug, id });
        }

        var saveViewData = await this.workspaceTicketsSaveViewService.BuildAsync(this.Workspace.Id, currentUserId, false, null);
        if (!saveViewData.CanEditTickets)
        {
            return this.Forbid();
        }

        try
        {
            var comment = await this.ticketCommentService.AddCommentAsync(
                this.Workspace.Id,
                id,
                currentUserId,
                this.NewCommentContent.Trim(),
                this.NewCommentIsVisibleToClient);

            var ticket = await this.ticketRepository.FindAsync(this.Workspace.Id, id);
            if (ticket != null)
            {
                await this.notificationTriggerService.NotifyTicketCommentAddedAsync(
                    this.Workspace.Id,
                    ticket,
                    currentUserId,
                    this.NewCommentIsVisibleToClient);
            }

            await this.BroadcastCommentAddedAsync(hub, comment);
            this.SetSuccessMessage("Comment added successfully.");
        }
        catch (Exception ex)
        {
            this.SetErrorMessage($"Failed to add comment: {ex.Message}");
        }

        return this.RedirectToPage("/Workspaces/TicketsDetails", new { slug, id });
    }

    private async Task BroadcastCommentAddedAsync(Microsoft.AspNetCore.SignalR.IHubContext<Realtime.TicketsHub> hub, TicketComment comment)
    {
        var group = Realtime.TicketsHub.WorkspaceGroup(this.WorkspaceSlug ?? string.Empty);
        await hub.Clients.Group(group).SendCoreAsync("commentAdded",
        [
            new
            {
                id = comment.Id,
                ticketId = comment.TicketId,
                content = comment.Content,
                isVisibleToClient = comment.IsVisibleToClient,
                createdByUserId = comment.CreatedByUserId,
                createdByUserName = comment.CreatedByUser?.Name ?? comment.CreatedByUser?.Email ?? "Unknown",
                createdAt = comment.CreatedAt
            }
        ]);
    }

    private int ExtractCurrentUserId() => this.TryGetUserId(out var uid) ? uid : InvalidTicketId;

    public async Task<IActionResult> OnPostSaveAsync(string slug, int id, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Realtime.TicketsHub> hub)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var workspaceId = this.Workspace.Id;
        var currentUserId = this.ExtractCurrentUserId();
        if (currentUserId == InvalidTicketId)
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(currentUserId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var inventories = this.ParseInventoriesFromJson();
        var resolvedId = this.ResolveTicketId(id);
        var isNew = resolvedId <= InvalidTicketId;
        var existing = !isNew ? await this.ticketRepository.FindAsync(workspaceId, resolvedId) : null;

        var saveViewData = await this.workspaceTicketsSaveViewService.BuildAsync(workspaceId, currentUserId, isNew, existing);
        var authCheck = this.ValidateTicketPermissions(isNew, saveViewData);
        if (authCheck != null)
        {
            return authCheck;
        }

        try
        {
            var ticket = isNew
                ? await this.HandleNewTicketAsync(workspaceId, currentUserId, inventories)
                : await this.HandleTicketUpdateAsync(workspaceId, resolvedId, currentUserId, existing, inventories);

            if (ticket == null)
            {
                return this.NotFound();
            }

            await this.BroadcastTicketChangeAsync(hub, ticket, isNew, workspaceId);
            this.SetSuccessMessage(SuccessTicketSaved);
            return this.RedirectToTicketsWithPreservedFilters(slug);
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
            return this.RedirectToPage("/Workspaces/Tickets", new { slug });
        }
    }

    private List<TicketInventory> ParseInventoriesFromJson()
    {
        var inventories = new List<TicketInventory>();
        if (!string.IsNullOrWhiteSpace(this.TicketInventoriesJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<TicketInventoryDto>>(this.TicketInventoriesJson);
                if (parsed != null)
                {
                    foreach (var dto in parsed)
                    {
                        inventories.Add(new TicketInventory
                        {
                            InventoryId = dto.Id,
                            Quantity = dto.Quantity,
                            UnitPrice = dto.UnitPrice
                        });
                    }
                }
            }
            catch { }
        }
        return inventories;
    }

    private int ResolveTicketId(int id)
    {
        var resolvedId = id;

        if (resolvedId <= InvalidTicketId && this.Request.RouteValues.TryGetValue("id", out var routeValue) && routeValue != null)
        {
            if (int.TryParse(routeValue.ToString(), out var rid) && rid > InvalidTicketId)
            {
                resolvedId = rid;
            }
        }

        if (resolvedId <= InvalidTicketId)
        {
            var formId = this.Request.Form["id"].ToString();
            if (int.TryParse(formId, out var formIdParsed) && formIdParsed > InvalidTicketId)
            {
                resolvedId = formIdParsed;
            }
        }

        if (resolvedId <= InvalidTicketId)
        {
            var queryId = this.Request.Query["id"].ToString();
            if (int.TryParse(queryId, out var queryIdParsed) && queryIdParsed > InvalidTicketId)
            {
                resolvedId = queryIdParsed;
            }
        }

        return resolvedId;
    }

    private ForbidResult? ValidateTicketPermissions(bool isNew, WorkspaceTicketsSaveViewData saveViewData)
    {
        if (isNew && !saveViewData.CanCreateTickets)
        {
            return this.Forbid();
        }

        if (!isNew && !saveViewData.CanEditTickets)
        {
            return this.Forbid();
        }

        if (!isNew && !saveViewData.CanAccessTicket)
        {
            return this.Forbid();
        }

        return null;
    }

    private async Task<Ticket?> HandleNewTicketAsync(int workspaceId, int userId, List<TicketInventory> inventories)
    {
        var createReq = new CreateTicketRequest
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = userId,
            Subject = (this.EditSubject ?? string.Empty).Trim(),
            Description = (this.EditDescription ?? string.Empty).Trim(),
            Type = DefaultOrTrim(this.EditType, DefaultTicketType),
            Priority = DefaultOrTrim(this.EditPriority, DefaultTicketPriority),
            Status = DefaultOrTrim(this.EditStatus, DefaultTicketStatus),
            ContactId = this.EditContactId,
            AssignedUserId = null,
            AssignedTeamId = null,
            LocationId = null,
            Inventories = inventories
        };

        var ticket = await this.ticketManagementService.CreateTicketAsync(createReq);
        await this.notificationTriggerService.NotifyTicketCreatedAsync(workspaceId, ticket, userId);
        return ticket;
    }

    private async Task<Ticket?> HandleTicketUpdateAsync(int workspaceId, int ticketId, int userId, Ticket? existing, List<TicketInventory> inventories)
    {
        if (this.EnsureEntityExistsOrNotFound(existing) is IActionResult result)
        {
            throw new InvalidOperationException(ErrorTicketNotFound);
        }

        var oldAssignedUserId = existing!.AssignedUserId;
        var oldAssignedTeamId = existing.AssignedTeamId;
        var oldStatusId = existing.StatusId;

        var updateReq = new UpdateTicketRequest
        {
            TicketId = ticketId,
            WorkspaceId = workspaceId,
            UpdatedByUserId = userId,
            Subject = this.EditSubject?.Trim(),
            Description = this.EditDescription?.Trim(),
            Type = this.EditType?.Trim(),
            Priority = this.EditPriority?.Trim(),
            Status = this.EditStatus?.Trim(),
            ContactId = this.EditContactId,
            AssignedUserId = null,
            AssignedTeamId = null,
            LocationId = null,
            Inventories = inventories
        };

        var ticket = await this.ticketManagementService.UpdateTicketAsync(updateReq);
        await this.NotifyTicketChangesAsync(workspaceId, ticket, userId, oldAssignedUserId, oldAssignedTeamId, oldStatusId);
        return ticket;
    }

    private async Task NotifyTicketChangesAsync(int workspaceId, Ticket ticket, int userId, int? oldAssignedUserId, int? oldAssignedTeamId, int? oldStatusId)
    {
        if (oldAssignedUserId != ticket.AssignedUserId || oldAssignedTeamId != ticket.AssignedTeamId)
        {
            await this.notificationTriggerService.NotifyTicketAssignmentChangedAsync(
                workspaceId, ticket, oldAssignedUserId, oldAssignedTeamId, userId);
        }

        if (oldStatusId != ticket.StatusId)
        {
            await this.notificationTriggerService.NotifyTicketStatusChangedAsync(
                workspaceId, ticket,
                oldStatusId?.ToString() ?? "Unknown",
                ticket.StatusId?.ToString() ?? "Unknown",
                userId);
        }
    }

    private async Task BroadcastTicketChangeAsync(Microsoft.AspNetCore.SignalR.IHubContext<Realtime.TicketsHub> hub, Ticket ticket, bool isNew, int workspaceId)
    {
        var assignedDisplay = ticket.AssignedUserId.HasValue
            ? await this.ticketManagementService.GetAssigneeDisplayNameAsync(ticket.AssignedUserId.Value)
            : null;

        var assignedTeamName = ticket.AssignedTeamId.HasValue
            ? (await this.teamRepository.FindByIdAsync(ticket.AssignedTeamId.Value))?.Name
            : null;

        var (invSummary, invDetails) = await this.ticketManagementService.GenerateInventorySummaryAsync(
            ticket.TicketInventories?.ToList() ?? [],
            workspaceId);

        var group = Realtime.TicketsHub.WorkspaceGroup(this.WorkspaceSlug ?? string.Empty);
        var eventName = isNew ? "ticketCreated" : "ticketUpdated";
        var ticketData = BuildTicketBroadcastObject(ticket, assignedDisplay, assignedTeamName, invSummary, invDetails, isNew);

        await hub.Clients.Group(group).SendCoreAsync(eventName, [ticketData]);
    }

    private static object BuildTicketBroadcastObject(Ticket ticket, string? assignedDisplay, string? assignedTeamName, string invSummary, string invDetails, bool isNew)
    {
        if (isNew)
        {
            return new
            {
                id = ticket.Id,
                subject = ticket.Subject ?? string.Empty,
                typeId = ticket.TicketTypeId,
                priorityId = ticket.PriorityId,
                statusId = ticket.StatusId,
                contactId = ticket.ContactId,
                assignedUserId = ticket.AssignedUserId,
                assignedDisplay,
                assignedTeamId = ticket.AssignedTeamId,
                assignedTeamName,
                inventorySummary = invSummary,
                inventoryDetails = invDetails,
                createdAt = ticket.CreatedAt
            };
        }

        return new
        {
            id = ticket.Id,
            subject = ticket.Subject ?? string.Empty,
            typeId = ticket.TicketTypeId,
            priorityId = ticket.PriorityId,
            statusId = ticket.StatusId,
            contactId = ticket.ContactId,
            assignedUserId = ticket.AssignedUserId,
            assignedDisplay,
            assignedTeamId = ticket.AssignedTeamId,
            assignedTeamName,
            inventorySummary = invSummary,
            inventoryDetails = invDetails,
            updatedAt = DateTime.UtcNow
        };
    }

    private RedirectToPageResult RedirectToTicketsWithPreservedFilters(string slug) => this.RedirectToPage("/Workspaces/Tickets", new
    {
        slug,
        Query = this.Request.Query["Query"].ToString(),
        Type = this.Request.Query["Type"].ToString(),
        Status = this.Request.Query["Status"].ToString(),
        Priority = this.Request.Query["Priority"].ToString(),
        ContactQuery = this.Request.Query["ContactQuery"].ToString(),
        AssigneeUserId = this.Request.Query["AssigneeUserId"].ToString(),
        AssigneeTeamName = this.Request.Query["AssigneeTeamName"].ToString(),
        Mine = this.Request.Query["Mine"].ToString(),
        PageNumber = this.Request.Query["PageNumber"].ToString(),
        PageSize = this.Request.Query["PageSize"].ToString()
    });

    private static string DefaultOrTrim(string? value, string defaultValue) => string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
}
