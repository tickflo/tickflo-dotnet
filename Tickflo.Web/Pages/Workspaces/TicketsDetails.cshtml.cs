using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using System.Linq;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class TicketsDetailsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IUserRepository _users;
    private readonly IUserWorkspaceRepository _userWorkspaces;
    private readonly IUserWorkspaceRoleRepository _roles;
    private readonly IHttpContextAccessor _http;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly ITeamRepository _teamRepo;

    public TicketsDetailsModel(IWorkspaceRepository workspaceRepo, ITicketRepository ticketRepo, IContactRepository contactRepo, IUserRepository users, IUserWorkspaceRepository userWorkspaces, IUserWorkspaceRoleRepository roles, IHttpContextAccessor http, ITicketStatusRepository statusRepo, ITicketPriorityRepository priorityRepo, ITicketTypeRepository typeRepo, ITicketHistoryRepository historyRepo, ITeamRepository teamRepo)
    {
        _workspaceRepo = workspaceRepo;
        _ticketRepo = ticketRepo;
        _contactRepo = contactRepo;
        _users = users;
        _userWorkspaces = userWorkspaces;
        _roles = roles;
        _http = http;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _historyRepo = historyRepo;
        _teamRepo = teamRepo;
    }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public Ticket? Ticket { get; private set; }
    public Contact? Contact { get; private set; }
    public IReadOnlyList<Contact> Contacts { get; private set; } = Array.Empty<Contact>();
    public bool IsWorkspaceAdmin { get; private set; }
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

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (id > 0)
        {
            Ticket = await _ticketRepo.FindAsync(Workspace.Id, id);
            if (Ticket == null) return NotFound();
            // Load history only for existing tickets
            History = await _historyRepo.ListForTicketAsync(Workspace.Id, id);
        }
        else
        {
            Ticket = new Ticket
            {
                WorkspaceId = Workspace.Id,
                Type = "Standard",
                Priority = "Normal",
                Status = "New"
            };
        }
        Contact = Ticket.ContactId.HasValue ? await _contactRepo.FindAsync(Workspace.Id, Ticket.ContactId.Value) : null;
        Contacts = await _contactRepo.ListAsync(Workspace.Id);
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        IsWorkspaceAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, Workspace.Id);
        var sts = await _statusRepo.ListAsync(Workspace.Id);
        if (sts.Count == 0)
        {
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
        // Load priorities
        var pris = await _priorityRepo.ListAsync(Workspace.Id);
        if (pris.Count == 0)
        {
            pris = new List<Tickflo.Core.Entities.TicketPriority>{
                new() { WorkspaceId = Workspace.Id, Name = "Low", Color = "warning", SortOrder = 1 },
                new() { WorkspaceId = Workspace.Id, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new() { WorkspaceId = Workspace.Id, Name = "High", Color = "error", SortOrder = 3 },
            };
        }
        Priorities = pris;
        PriorityColorByName = pris.GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        // Load types
        var types = await _typeRepo.ListAsync(Workspace.Id);
        if (types.Count == 0)
        {
            types = new List<Tickflo.Core.Entities.TicketType>{
                new() { WorkspaceId = Workspace.Id, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new() { WorkspaceId = Workspace.Id, Name = "Bug", Color = "error", SortOrder = 2 },
                new() { WorkspaceId = Workspace.Id, Name = "Feature", Color = "primary", SortOrder = 3 },
            };
        }
        Types = types;
        TypeColorByName = types.GroupBy(t => t.Name)
            .ToDictionary(g => g.Key, g => string.IsNullOrWhiteSpace(g.Last().Color) ? "neutral" : g.Last().Color);

        var memberships = await _userWorkspaces.FindForWorkspaceAsync(Workspace.Id);
        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        foreach (var uid2 in userIds)
        {
            var u = await _users.FindByIdAsync(uid2);
            if (u != null) Members.Add(u);
        }
        Teams = await _teamRepo.ListForWorkspaceAsync(Workspace.Id);
        return Page();
    }

    // Consolidated save: updates subject, description, priority, status, and assignment
    public async Task<IActionResult> OnPostSaveAsync(string slug, int id, int? assignedUserId, int? assignedTeamId, [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<Tickflo.Web.Realtime.TicketsHub> hub)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var workspaceId = Workspace.Id;
        var uidStr = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = int.TryParse(uidStr, out var uid) && await _roles.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();
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
        Ticket? t = null;
        if (isNew)
        {
            t = new Ticket
            {
                WorkspaceId = workspaceId,
                Subject = (EditSubject ?? string.Empty).Trim(),
                Description = (EditDescription ?? string.Empty).Trim(),
                Type = DefaultOrTrim(EditType, "Standard"),
                Priority = DefaultOrTrim(EditPriority, "Normal"),
                Status = DefaultOrTrim(EditStatus, "New"),
                InventoryRef = string.IsNullOrWhiteSpace(EditInventoryRef) ? null : EditInventoryRef!.Trim()
            };
            // Contact
            if (EditContactId.HasValue)
            {
                var c = await _contactRepo.FindAsync(workspaceId, EditContactId.Value);
                if (c != null) t.ContactId = EditContactId.Value;
            }
            // Assignment
            if (assignedUserId.HasValue)
            {
                var memberships = await _userWorkspaces.FindForWorkspaceAsync(workspaceId);
                if (!memberships.Any(m => m.UserId == assignedUserId.Value)) return BadRequest("User not in workspace");
                t.AssignedUserId = assignedUserId.Value;
            }
            if (assignedTeamId.HasValue)
            {
                var team = await _teamRepo.FindByIdAsync(assignedTeamId.Value);
                if (team == null || team.WorkspaceId != workspaceId) return BadRequest("Invalid team");
                t.AssignedTeamId = assignedTeamId.Value;
            }
            await _ticketRepo.CreateAsync(t);
            // History: created
            await _historyRepo.CreateAsync(new TicketHistory
            {
                WorkspaceId = workspaceId,
                TicketId = t.Id,
                CreatedByUserId = uid,
                Action = "created",
                Note = "Ticket created"
            });
        }
        else
        {
            t = await _ticketRepo.FindAsync(workspaceId, resolvedId);
            if (t == null) return NotFound();
            // Snapshot old values for diff
            var old = new {
                t.Subject,
                t.Description,
                t.Type,
                t.Priority,
                t.Status,
                t.InventoryRef,
                t.ContactId,
                t.AssignedUserId
            };
            #pragma warning disable CS8602 // Dereference of a possibly null reference
            var subjectTrim = EditSubject?.Trim();
            if (!string.IsNullOrEmpty(subjectTrim)) t.Subject = subjectTrim;
            var descriptionTrim = EditDescription?.Trim();
            if (!string.IsNullOrEmpty(descriptionTrim)) t.Description = descriptionTrim;
            var typeTrim = EditType?.Trim();
            if (!string.IsNullOrEmpty(typeTrim)) t.Type = typeTrim;
            var priorityTrim = EditPriority?.Trim();
            if (!string.IsNullOrEmpty(priorityTrim)) t.Priority = priorityTrim;
            var statusTrim = EditStatus?.Trim();
            if (!string.IsNullOrEmpty(statusTrim)) t.Status = statusTrim;
            var inventoryRefTrim = EditInventoryRef?.Trim();
            t.InventoryRef = string.IsNullOrEmpty(inventoryRefTrim) ? null : inventoryRefTrim;
            #pragma warning restore CS8602
            // Update contact if provided
            if (EditContactId.HasValue)
            {
                var c = await _contactRepo.FindAsync(workspaceId, EditContactId.Value);
                if (c != null) t.ContactId = EditContactId.Value;
            }
            // Assignment (optional)
            if (assignedUserId.HasValue)
            {
                var memberships = await _userWorkspaces.FindForWorkspaceAsync(workspaceId);
                if (!memberships.Any(m => m.UserId == assignedUserId.Value)) return BadRequest("User not in workspace");
                t.AssignedUserId = assignedUserId.Value;
            }
            if (assignedTeamId.HasValue)
            {
                var team = await _teamRepo.FindByIdAsync(assignedTeamId.Value);
                if (team == null || team.WorkspaceId != workspaceId) return BadRequest("Invalid team");
                t.AssignedTeamId = assignedTeamId.Value;
            }
            await _ticketRepo.UpdateAsync(t);
            // History: per-field changes
            async Task logIfChanged(string field, string? oldVal, string? newVal)
            {
                var o = (oldVal ?? string.Empty).Trim();
                var n = (newVal ?? string.Empty).Trim();
                if (o == n) return;
                await _historyRepo.CreateAsync(new TicketHistory
                {
                    WorkspaceId = workspaceId,
                    TicketId = t.Id,
                    CreatedByUserId = uid,
                    Action = "field_changed",
                    Field = field,
                    OldValue = o == string.Empty ? null : o,
                    NewValue = n == string.Empty ? null : n
                });
            }
            await logIfChanged("Subject", old.Subject, t.Subject);
            await logIfChanged("Description", old.Description, t.Description);
            await logIfChanged("Type", old.Type, t.Type);
            await logIfChanged("Priority", old.Priority, t.Priority);
            await logIfChanged("Status", old.Status, t.Status);
            await logIfChanged("InventoryRef", old.InventoryRef, t.InventoryRef);
            await logIfChanged("ContactId", old.ContactId?.ToString(), t.ContactId?.ToString());
            await logIfChanged("AssignedUserId", old.AssignedUserId?.ToString(), t.AssignedUserId?.ToString());
        }
        // Broadcast update to workspace clients
        string? assignedDisplay = null;
        if (t.AssignedUserId.HasValue)
        {
            var au = await _users.FindByIdAsync(t.AssignedUserId.Value);
            if (au != null)
            {
                var nameVal = (au.Name ?? string.Empty).Trim();
                var emailVal = (au.Email ?? string.Empty).Trim();
                var name = string.IsNullOrEmpty(nameVal) ? "(unknown)" : nameVal;
                var email = emailVal;
                assignedDisplay = string.IsNullOrEmpty(email) ? name : $"{name} ({email})";
            }
        }
        string? assignedTeamName = null;
        if (t.AssignedTeamId.HasValue)
        {
            var team = await _teamRepo.FindByIdAsync(t.AssignedTeamId.Value);
            assignedTeamName = team?.Name;
        }
        var group = Tickflo.Web.Realtime.TicketsHub.WorkspaceGroup(WorkspaceSlug ?? string.Empty);
        if (isNew)
        {
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
                    inventoryRef = t.InventoryRef,
                    createdAt = t.CreatedAt
                }
            });
        }
        else
        {
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
                    inventoryRef = t.InventoryRef,
                    updatedAt = DateTime.UtcNow
                }
            });
        }
        TempData["Success"] = "Ticket saved.";
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
