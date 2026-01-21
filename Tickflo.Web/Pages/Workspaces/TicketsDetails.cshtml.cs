using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Tickets;
using Tickflo.Core.Services.Notifications;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TicketsDetailsModel : WorkspacePageModel
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

    private class TicketInventoryDto
    {
        public int id { get; set; }
        public string sku { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int quantity { get; set; }
        public decimal unitPrice { get; set; }
    }
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketManagementService _ticketService;
    private readonly IWorkspaceTicketDetailsViewService _viewService;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ITeamRepository _teamRepo;
    private readonly IUserWorkspaceRoleRepository _roles;
    private readonly IWorkspaceTicketsSaveViewService _savingViewService;
    private readonly ITicketCommentService _commentService;
    private readonly IUserRepository _userRepo;
    private readonly INotificationTriggerService _notificationTrigger;

    public TicketsDetailsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, ITicketRepository ticketRepo, ITicketManagementService ticketService, IWorkspaceTicketDetailsViewService viewService, IRolePermissionRepository rolePerms, ITeamRepository teamRepo, IUserWorkspaceRoleRepository roles, IWorkspaceTicketsSaveViewService savingViewService, ITicketCommentService commentService, IUserRepository userRepo, INotificationTriggerService notificationTrigger)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _ticketRepo = ticketRepo;
        _ticketService = ticketService;
        _viewService = viewService;
        _rolePerms = rolePerms;
        _teamRepo = teamRepo;
        _roles = roles;
        _savingViewService = savingViewService;
        _commentService = commentService;
        _userRepo = userRepo;
        _notificationTrigger = notificationTrigger;
    }
    public List<Inventory> InventoryItems { get; private set; } = new();

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Ticket? Ticket { get; private set; }
    public Contact? Contact { get; private set; }
    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();
    public bool IsWorkspaceAdmin { get; private set; }
    public bool CanViewTickets { get; private set; }
    public bool CanEditTickets { get; private set; }
    public bool CanCreateTickets { get; private set; }
    public string? TicketViewScope { get; private set; }
    public List<User> Members { get; private set; } = new();
    public IReadOnlyList<Tickflo.Core.Entities.TicketStatus> Statuses { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketStatus>();
    public Dictionary<string,string> StatusColorByName { get; private set; } = new();
    public IReadOnlyList<Tickflo.Core.Entities.TicketPriority> Priorities { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketPriority>();
    public Dictionary<string,string> PriorityColorByName { get; private set; } = new();
    public IReadOnlyList<Tickflo.Core.Entities.TicketType> Types { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketType>();
    public Dictionary<string,string> TypeColorByName { get; private set; } = new();
    public IReadOnlyList<TicketHistory> History { get; private set; } = Array.Empty<TicketHistory>();
    public List<Team> Teams { get; private set; } = new();
    [BindProperty(SupportsGet = true)]
    public int? LocationId { get; set; }
    public List<Location> LocationOptions { get; private set; } = new();

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
    
    public IReadOnlyList<TicketComment> Comments { get; private set; } = Array.Empty<TicketComment>();
    [BindProperty]
    public string? NewCommentContent { get; set; }
    [BindProperty]
    public bool NewCommentIsVisibleToClient { get; set; } = false;

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;
        if (Workspace == null) return NotFound();

        var viewData = await _viewService.BuildAsync(Workspace.Id, id, currentUserId, LocationId);
        if (viewData == null) return Forbid();

        LoadViewDataFromService(viewData);
        LocationId = Ticket?.LocationId;

        if (Ticket != null && Ticket.Id > InvalidTicketId)
        {
            Comments = await _commentService.GetCommentsAsync(Workspace.Id, Ticket.Id, isClientView: false);
        }

        return Page();
    }

    private void LoadViewDataFromService(WorkspaceTicketDetailsViewData viewData)
    {
        Ticket = viewData.Ticket;
        Contact = viewData.Contact;
        Contacts = viewData.Contacts;
        Statuses = viewData.Statuses;
        StatusColorByName = viewData.StatusColorByName;
        Priorities = viewData.Priorities;
        PriorityColorByName = viewData.PriorityColorByName;
        Types = viewData.Types;
        TypeColorByName = viewData.TypeColorByName;
        History = viewData.History;
        Members = viewData.Members;
        Teams = viewData.Teams;
        InventoryItems = viewData.InventoryItems;
        LocationOptions = viewData.LocationOptions;
        CanViewTickets = viewData.CanViewTickets;
        CanEditTickets = viewData.CanEditTickets;
        CanCreateTickets = viewData.CanCreateTickets;
        IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        TicketViewScope = viewData.TicketViewScope;
    }

    public async Task<IActionResult> OnPostAddCommentAsync(string slug, int id, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        var currentUserId = ExtractCurrentUserId();
        if (currentUserId == InvalidTicketId) return Forbid();

        if (string.IsNullOrWhiteSpace(NewCommentContent))
        {
            SetErrorMessage(ErrorCommentEmpty);
            return RedirectToPage("/Workspaces/TicketsDetails", new { slug, id });
        }

        var saveViewData = await _savingViewService.BuildAsync(Workspace.Id, currentUserId, false, null);
        if (!saveViewData.CanEditTickets) return Forbid();

        try
        {
            var comment = await _commentService.AddCommentAsync(
                Workspace.Id,
                id,
                currentUserId,
                NewCommentContent.Trim(),
                NewCommentIsVisibleToClient);

            var ticket = await _ticketRepo.FindAsync(Workspace.Id, id);
            if (ticket != null)
            {
                await _notificationTrigger.NotifyTicketCommentAddedAsync(
                    Workspace.Id,
                    ticket,
                    currentUserId,
                    NewCommentIsVisibleToClient);
            }

            await BroadcastCommentAddedAsync(hub, comment);
            SetSuccessMessage("Comment added successfully.");
        }
        catch (Exception ex)
        {
            SetErrorMessage($"Failed to add comment: {ex.Message}");
        }

        return RedirectToPage("/Workspaces/TicketsDetails", new { slug, id });
    }

    private async Task BroadcastCommentAddedAsync(Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub, TicketComment comment)
    {
        var group = Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug ?? string.Empty);
        await hub.Clients.Group(group).SendCoreAsync("commentAdded", new object[]
        {
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
        });
    }

    private int ExtractCurrentUserId()
    {
        return TryGetUserId(out var uid) ? uid : InvalidTicketId;
    }

    public async Task<IActionResult> OnPostSaveAsync(string slug, int id, int? assignedUserId, int? assignedTeamId, int? locationId, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        var workspaceId = Workspace.Id;
        var currentUserId = ExtractCurrentUserId();
        if (currentUserId == InvalidTicketId) return Forbid();

        var inventories = ParseInventoriesFromJson();
        var resolvedId = ResolveTicketId(id);
        var isNew = resolvedId <= InvalidTicketId;
        var existing = !isNew ? await _ticketRepo.FindAsync(workspaceId, resolvedId) : null;

        var saveViewData = await _savingViewService.BuildAsync(workspaceId, currentUserId, isNew, existing);
        var authCheck = ValidateTicketPermissions(isNew, saveViewData);
        if (authCheck != null) return authCheck;

        try
        {
            var ticket = isNew
                ? await HandleNewTicketAsync(workspaceId, currentUserId, inventories)
                : await HandleTicketUpdateAsync(workspaceId, resolvedId, currentUserId, existing, inventories);

            if (ticket == null) return NotFound();

            await BroadcastTicketChangeAsync(hub, ticket, isNew, workspaceId);
            SetSuccessMessage(SuccessTicketSaved);
            return RedirectToTicketsWithPreservedFilters(slug);
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
            return RedirectToPage("/Workspaces/Tickets", new { slug });
        }
    }

    private List<TicketInventory> ParseInventoriesFromJson()
    {
        var inventories = new List<TicketInventory>();
        if (!string.IsNullOrWhiteSpace(TicketInventoriesJson))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<TicketInventoryDto>>(TicketInventoriesJson);
                if (parsed != null)
                {
                    foreach (var dto in parsed)
                    {
                        inventories.Add(new TicketInventory
                        {
                            InventoryId = dto.id,
                            Quantity = dto.quantity,
                            UnitPrice = dto.unitPrice
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

        if (resolvedId <= InvalidTicketId && Request.RouteValues.TryGetValue("id", out var routeValue) && routeValue != null)
        {
            if (int.TryParse(routeValue.ToString(), out var rid) && rid > InvalidTicketId)
                resolvedId = rid;
        }

        if (resolvedId <= InvalidTicketId)
        {
            var formId = Request.Form["id"].ToString();
            if (int.TryParse(formId, out var formIdParsed) && formIdParsed > InvalidTicketId)
                resolvedId = formIdParsed;
        }

        if (resolvedId <= InvalidTicketId)
        {
            var queryId = Request.Query["id"].ToString();
            if (int.TryParse(queryId, out var queryIdParsed) && queryIdParsed > InvalidTicketId)
                resolvedId = queryIdParsed;
        }

        return resolvedId;
    }

    private IActionResult? ValidateTicketPermissions(bool isNew, WorkspaceTicketsSaveViewData saveViewData)
    {
        if (isNew && !saveViewData.CanCreateTickets) return Forbid();
        if (!isNew && !saveViewData.CanEditTickets) return Forbid();
        if (!isNew && !saveViewData.CanAccessTicket) return Forbid();
        return null;
    }

    private async Task<Ticket?> HandleNewTicketAsync(int workspaceId, int userId, List<TicketInventory> inventories)
    {
        var createReq = new CreateTicketRequest
        {
            WorkspaceId = workspaceId,
            CreatedByUserId = userId,
            Subject = (EditSubject ?? string.Empty).Trim(),
            Description = (EditDescription ?? string.Empty).Trim(),
            Type = DefaultOrTrim(EditType, DefaultTicketType),
            Priority = DefaultOrTrim(EditPriority, DefaultTicketPriority),
            Status = DefaultOrTrim(EditStatus, DefaultTicketStatus),
            ContactId = EditContactId,
            AssignedUserId = null,
            AssignedTeamId = null,
            LocationId = null,
            Inventories = inventories
        };

        var ticket = await _ticketService.CreateTicketAsync(createReq);
        await _notificationTrigger.NotifyTicketCreatedAsync(workspaceId, ticket, userId);
        return ticket;
    }

    private async Task<Ticket?> HandleTicketUpdateAsync(int workspaceId, int ticketId, int userId, Ticket? existing, List<TicketInventory> inventories)
    {
        if (EnsureEntityExistsOrNotFound(existing) is IActionResult result) throw new InvalidOperationException(ErrorTicketNotFound);

        var oldAssignedUserId = existing!.AssignedUserId;
        var oldAssignedTeamId = existing.AssignedTeamId;
        var oldStatusId = existing.StatusId;

        var updateReq = new UpdateTicketRequest
        {
            TicketId = ticketId,
            WorkspaceId = workspaceId,
            UpdatedByUserId = userId,
            Subject = EditSubject?.Trim(),
            Description = EditDescription?.Trim(),
            Type = EditType?.Trim(),
            Priority = EditPriority?.Trim(),
            Status = EditStatus?.Trim(),
            ContactId = EditContactId,
            AssignedUserId = null,
            AssignedTeamId = null,
            LocationId = null,
            Inventories = inventories
        };

        var ticket = await _ticketService.UpdateTicketAsync(updateReq);
        await NotifyTicketChangesAsync(workspaceId, ticket, userId, oldAssignedUserId, oldAssignedTeamId, oldStatusId);
        return ticket;
    }

    private async Task NotifyTicketChangesAsync(int workspaceId, Ticket ticket, int userId, int? oldAssignedUserId, int? oldAssignedTeamId, int? oldStatusId)
    {
        if (oldAssignedUserId != ticket.AssignedUserId || oldAssignedTeamId != ticket.AssignedTeamId)
        {
            await _notificationTrigger.NotifyTicketAssignmentChangedAsync(
                workspaceId, ticket, oldAssignedUserId, oldAssignedTeamId, userId);
        }

        if (oldStatusId != ticket.StatusId)
        {
            await _notificationTrigger.NotifyTicketStatusChangedAsync(
                workspaceId, ticket,
                oldStatusId?.ToString() ?? "Unknown",
                ticket.StatusId?.ToString() ?? "Unknown",
                userId);
        }
    }

    private async Task BroadcastTicketChangeAsync(Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub, Ticket ticket, bool isNew, int workspaceId)
    {
        var assignedDisplay = ticket.AssignedUserId.HasValue
            ? await _ticketService.GetAssigneeDisplayNameAsync(ticket.AssignedUserId.Value)
            : null;

        var assignedTeamName = ticket.AssignedTeamId.HasValue
            ? (await _teamRepo.FindByIdAsync(ticket.AssignedTeamId.Value))?.Name
            : null;

        var (invSummary, invDetails) = await _ticketService.GenerateInventorySummaryAsync(
            ticket.TicketInventories?.ToList() ?? new List<TicketInventory>(),
            workspaceId);

        var group = Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug ?? string.Empty);
        var eventName = isNew ? "ticketCreated" : "ticketUpdated";
        var ticketData = BuildTicketBroadcastObject(ticket, assignedDisplay, assignedTeamName, invSummary, invDetails, isNew);

        await hub.Clients.Group(group).SendCoreAsync(eventName, new object[] { ticketData });
    }

    private object BuildTicketBroadcastObject(Ticket ticket, string? assignedDisplay, string? assignedTeamName, string invSummary, string invDetails, bool isNew)
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
                assignedDisplay = assignedDisplay,
                assignedTeamId = ticket.AssignedTeamId,
                assignedTeamName = assignedTeamName,
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
            assignedDisplay = assignedDisplay,
            assignedTeamId = ticket.AssignedTeamId,
            assignedTeamName = assignedTeamName,
            inventorySummary = invSummary,
            inventoryDetails = invDetails,
            updatedAt = DateTime.UtcNow
        };
    }

    private RedirectToPageResult RedirectToTicketsWithPreservedFilters(string slug)
    {
        return RedirectToPage("/Workspaces/Tickets", new
        {
            slug,
            Query = Request.Query["Query"].ToString(),
            Type = Request.Query["Type"].ToString(),
            Status = Request.Query["Status"].ToString(),
            Priority = Request.Query["Priority"].ToString(),
            ContactQuery = Request.Query["ContactQuery"].ToString(),
            AssigneeUserId = Request.Query["AssigneeUserId"].ToString(),
            AssigneeTeamName = Request.Query["AssigneeTeamName"].ToString(),
            Mine = Request.Query["Mine"].ToString(),
            PageNumber = Request.Query["PageNumber"].ToString(),
            PageSize = Request.Query["PageSize"].ToString()
        });
    }

    private static string DefaultOrTrim(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value!.Trim();
    }
}
