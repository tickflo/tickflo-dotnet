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

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class TicketsDetailsModel : WorkspacePageModel
{
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
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketManagementService _ticketService;
    private readonly IWorkspaceTicketDetailsViewService _viewService;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly ITeamRepository _teamRepo;
    private readonly IUserWorkspaceRoleRepository _roles;
    private readonly IWorkspaceTicketsSaveViewService _savingViewService;
    private readonly ITicketCommentService _commentService;
    private readonly IUserRepository _userRepo;

    public TicketsDetailsModel(IWorkspaceRepository workspaceRepo, ITicketRepository ticketRepo, ITicketManagementService ticketService, IWorkspaceTicketDetailsViewService viewService, IRolePermissionRepository rolePerms, ITeamRepository teamRepo, IUserWorkspaceRoleRepository roles, IWorkspaceTicketsSaveViewService savingViewService, ITicketCommentService commentService, IUserRepository userRepo)
    {
        _workspaceRepo = workspaceRepo;
        _ticketRepo = ticketRepo;
        _ticketService = ticketService;
        _viewService = viewService;
        _rolePerms = rolePerms;
        _teamRepo = teamRepo;
        _roles = roles;
        _savingViewService = savingViewService;
        _commentService = commentService;
        _userRepo = userRepo;
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
    
    // Comment properties
    public IReadOnlyList<TicketComment> Comments { get; private set; } = Array.Empty<TicketComment>();
    [BindProperty]
    public string? NewCommentContent { get; set; }
    [BindProperty]
    public bool NewCommentIsVisibleToClient { get; set; } = false;

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;
        var currentUserId = TryGetUserId(out var uid) ? uid : 0;

        // Load view data - this handles permissions, scope, metadata
        var viewData = await _viewService.BuildAsync(Workspace.Id, id, currentUserId, LocationId);
        if (viewData == null) return Forbid();

        // Populate properties from view data
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
        LocationId = Ticket?.LocationId;

        // Load comments for the ticket if it exists
        if (Ticket != null && Ticket.Id > 0)
        {
            Comments = await _commentService.GetCommentsAsync(Workspace.Id, Ticket.Id, isClientView: false);
        }

        return Page();
    }

    // Add a new comment to the ticket
    public async Task<IActionResult> OnPostAddCommentAsync(string slug, int id, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        var currentUserId = TryGetUserId(out var uid) ? uid : 0;
        if (currentUserId == 0) return Forbid();

        // Validate comment content
        if (string.IsNullOrWhiteSpace(NewCommentContent))
        {
            SetErrorMessage("Comment cannot be empty.");
            return RedirectToPage("/Workspaces/TicketsDetails", new { slug, id });
        }

        // Verify user can edit tickets (permission to add comments)
        var saveViewData = await _savingViewService.BuildAsync(Workspace.Id, uid, false, null);
        if (!saveViewData.CanEditTickets)
        {
            return Forbid();
        }

        try
        {
            // Add the comment
            var comment = await _commentService.AddCommentAsync(
                Workspace.Id,
                id,
                currentUserId,
                NewCommentContent.Trim(),
                NewCommentIsVisibleToClient
            );

            // Broadcast comment creation to workspace clients
            var group = Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug ?? string.Empty);
            
            await hub.Clients.Group(group).SendCoreAsync("commentAdded", new object[] {
                new {
                    id = comment.Id,
                    ticketId = comment.TicketId,
                    content = comment.Content,
                    isVisibleToClient = comment.IsVisibleToClient,
                    createdByUserId = comment.CreatedByUserId,
                    createdByUserName = comment.CreatedByUser?.Name ?? comment.CreatedByUser?.Email ?? "Unknown",
                    createdAt = comment.CreatedAt
                }
            });

            SetSuccessMessage("Comment added successfully.");
        }
        catch (Exception ex)
        {
            SetErrorMessage($"Failed to add comment: {ex.Message}");
        }

        return RedirectToPage("/Workspaces/TicketsDetails", new { slug, id });
    }

    // Consolidated save: updates subject, description, priority, status, and assignment
        public async Task<IActionResult> OnPostSaveAsync(string slug, int id, int? assignedUserId, int? assignedTeamId, int? locationId, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
        {
            // Bind inventory products from JSON
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
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var workspaceId = Workspace.Id;
        int uid = TryGetUserId(out var currentUid) ? currentUid : 0;
        // Robustly resolve ticket id: route -> form -> query
        int resolvedId = id;
        if (resolvedId <= 0)
        {
            var routeIdObj = Request.RouteValues.TryGetValue("id", out var rv) ? rv : null;
            if (routeIdObj != null && int.TryParse(routeIdObj.ToString(), out var rid) && rid > 0)
            {
                resolvedId = rid;
            }
        }
        if (resolvedId <= 0)
        {
            var idStr = Request.Form["id"].ToString();
            if (int.TryParse(idStr, out var parsed) && parsed > 0)
            {
                resolvedId = parsed;
            }
        }
        if (resolvedId <= 0)
        {
            var qStr = Request.Query["id"].ToString();
            if (int.TryParse(qStr, out var qid) && qid > 0)
            {
                resolvedId = qid;
            }
        }
        var isNew = resolvedId <= 0;
        var existing = !isNew ? await _ticketRepo.FindAsync(workspaceId, resolvedId) : null;
        
        var saveViewData = await _savingViewService.BuildAsync(workspaceId, uid, isNew, existing);
        if (isNew && !saveViewData.CanCreateTickets) return Forbid();
        if (!isNew && !saveViewData.CanEditTickets) return Forbid();
        if (!isNew && !saveViewData.CanAccessTicket) return Forbid();
        
        Ticket? t = null;
        if (isNew)
        {
            var createReq = new CreateTicketRequest
            {
                WorkspaceId = workspaceId,
                CreatedByUserId = uid,
                Subject = (EditSubject ?? string.Empty).Trim(),
                Description = (EditDescription ?? string.Empty).Trim(),
                Type = DefaultOrTrim(EditType, "Standard"),
                Priority = DefaultOrTrim(EditPriority, "Normal"),
                Status = DefaultOrTrim(EditStatus, "New"),
                ContactId = EditContactId,
                AssignedUserId = assignedUserId,
                AssignedTeamId = assignedTeamId,
                LocationId = locationId,
                Inventories = inventories
            };
            try
            {
                t = await _ticketService.CreateTicketAsync(createReq);
            }
            catch (InvalidOperationException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToPage("/Workspaces/Tickets", new { slug });
            }
        }
        else
        {
            existing = await _ticketRepo.FindAsync(workspaceId, resolvedId);
            if (EnsureEntityExistsOrNotFound(existing) is IActionResult ticketCheck) return ticketCheck;

            var updateReq = new UpdateTicketRequest
            {
                TicketId = resolvedId,
                WorkspaceId = workspaceId,
                UpdatedByUserId = uid,
                Subject = EditSubject?.Trim(),
                Description = EditDescription?.Trim(),
                Type = EditType?.Trim(),
                Priority = EditPriority?.Trim(),
                Status = EditStatus?.Trim(),
                ContactId = EditContactId,
                AssignedUserId = assignedUserId,
                AssignedTeamId = assignedTeamId,
                LocationId = locationId,
                Inventories = inventories
            };
            try
            {
                t = await _ticketService.UpdateTicketAsync(updateReq);
            }
            catch (InvalidOperationException ex)
            {
                SetErrorMessage(ex.Message);
                return RedirectToPage("/Workspaces/Tickets", new { slug });
            }
        }
        // Broadcast update to workspace clients
        string? assignedDisplay = t.AssignedUserId.HasValue ? await _ticketService.GetAssigneeDisplayNameAsync(t.AssignedUserId.Value) : null;
        string? assignedTeamName = null;
        if (t.AssignedTeamId.HasValue)
        {
            var team = await _teamRepo.FindByIdAsync(t.AssignedTeamId.Value);
            assignedTeamName = team?.Name;
        }
        var group = Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug ?? string.Empty);
        if (isNew)
        {
            var (invSummaryNew, invDetailsNew) = await _ticketService.GenerateInventorySummaryAsync(t.TicketInventories?.ToList() ?? new List<TicketInventory>(), workspaceId);
            await hub.Clients.Group(group).SendCoreAsync("ticketCreated", new object[] {
                new {
                    id = t.Id,
                    subject = t.Subject ?? string.Empty,
                    type = t.Type ?? "Standard",
                    priority = t.Priority ?? "Normal",
                    status = t.Status ?? "New",
                    contactId = t.ContactId,
                    assignedUserId = t.AssignedUserId,
                    assignedDisplay = assignedDisplay,
                    assignedTeamId = t.AssignedTeamId,
                    assignedTeamName = assignedTeamName,
                    inventorySummary = invSummaryNew,
                    inventoryDetails = invDetailsNew,
                    createdAt = t.CreatedAt
                }
            });
        }
        else
        {
            var (invSummaryUpd, invDetailsUpd) = await _ticketService.GenerateInventorySummaryAsync(t.TicketInventories?.ToList() ?? new List<TicketInventory>(), workspaceId);
            await hub.Clients.Group(group).SendCoreAsync("ticketUpdated", new object[] {
                new {
                    id = t.Id,
                    subject = t.Subject ?? string.Empty,
                    type = t.Type ?? "Standard",
                    priority = t.Priority ?? "Normal",
                    status = t.Status ?? "New",
                    contactId = t.ContactId,
                    assignedUserId = t.AssignedUserId,
                    assignedDisplay = assignedDisplay,
                    assignedTeamId = t.AssignedTeamId,
                    assignedTeamName = assignedTeamName,
                    inventorySummary = invSummaryUpd,
                    inventoryDetails = invDetailsUpd,
                    updatedAt = DateTime.UtcNow
                }
            });
        }
        SetSuccessMessage("Ticket saved.");
        // Preserve common filters when returning
        var statusQ = Request.Query["Status"].ToString();
        var priorityQ = Request.Query["Priority"].ToString();
        var contactQ = Request.Query["ContactQuery"].ToString();
        var assigneeQ = Request.Query["AssigneeUserId"].ToString();
        var mineQ = Request.Query["Mine"].ToString();
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        var typeQ = Request.Query["Type"].ToString();
        var pageSizeQ = Request.Query["PageSize"].ToString();
        var teamNameQ = Request.Query["AssigneeTeamName"].ToString();
        return RedirectToPage("/Workspaces/Tickets", new { slug, Query = queryQ, Type = typeQ, Status = statusQ, Priority = priorityQ, ContactQuery = contactQ, AssigneeUserId = assigneeQ, AssigneeTeamName = teamNameQ, Mine = mineQ, PageNumber = pageQ, PageSize = pageSizeQ });
    }

    private static string DefaultOrTrim(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value!.Trim();
    }
}
